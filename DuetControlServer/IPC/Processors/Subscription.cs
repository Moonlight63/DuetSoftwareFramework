﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DuetAPI;
using DuetAPI.Commands;
using DuetAPI.Connection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;

namespace DuetControlServer.IPC.Processors
{
    /// <summary>
    /// Subscription processor that notifies clients about object model changes.
    /// There is no point in deserializing the object model here so only the JSON representation is kept here.
    /// </summary>
    public class Subscription : Base
    {
        /// <summary>
        /// List of supported commands in this mode
        /// </summary>
        public static readonly Type[] SupportedCommands =
        {
            typeof(Acknowledge)
        };
        
        private static readonly ConcurrentDictionary<Subscription, SubscriptionMode> _subscriptions = new ConcurrentDictionary<Subscription, SubscriptionMode>();

        private readonly SubscriptionMode _mode;
        private JObject _jsonModel, _lastModel;
        private AsyncAutoResetEvent _updateAvailableEvent = new AsyncAutoResetEvent();
        
        /// <summary>
        /// Constructor of the subscription processor
        /// </summary>
        /// <param name="conn">Connection instance</param>
        /// <param name="initMessage">Initialization message</param>
        public Subscription(Connection conn, ClientInitMessage initMessage) : base(conn, initMessage)
        {
            _mode = (initMessage as SubscribeInitMessage).SubscriptionMode;
        }
        
        /// <summary>
        /// Task that keeps pushing model updates to the client
        /// </summary>
        /// <returns>Task that represents the lifecycle of a connection</returns>
        public override async Task Process()
        {
            // Initialize the machine model and register this subscriber
            using (await Model.Provider.AccessReadOnly())
            {
                _jsonModel = JObject.FromObject(Model.Provider.Get, JsonHelper.DefaultSerializer);
            }
            _lastModel = _jsonModel;
            _subscriptions.TryAdd(this, _mode);

            // Send over the full machine model initially and keep on sending updates
            try
            {
                string json;
                lock (_jsonModel)
                {
                    json = _jsonModel.ToString(Formatting.None);
                }
                await Connection.Send(json + "\n");
                
                do
                {
                    // Wait for acknowledgement
                    BaseCommand command = await Connection.ReceiveCommand();
                    if (!SupportedCommands.Contains(command.GetType()))
                    {
                        throw new ArgumentException($"Invalid command {command.Command} (wrong mode?)");
                    }

                    // Wait for another update
                    await _updateAvailableEvent.WaitAsync(Program.CancelSource.Token);
                    
                    // Send over the next update
                    if (_mode == SubscriptionMode.Full)
                    {
                        // Send the entire object model in Full mode
                        lock (_jsonModel)
                        {
                            json = _jsonModel.ToString(Formatting.None);
                        }
                        await Connection.Send(json + "\n");
                    }
                    else
                    {
                        // Only create a patch in Patch mode
                        JObject patch;
                        lock (_jsonModel)
                        {
                            patch = JsonHelper.DiffObject(_lastModel, _jsonModel);
                        }
                        
                        // Send it over
                        json = patch.ToString(Formatting.None);
                        await Connection.Send(json + "\n");
                    }
                } while (!Program.CancelSource.IsCancellationRequested);
            }
            catch (Exception e)
            {
                if (Connection.IsConnected)
                {
                    // Inform the client about this error
                    await Connection.SendResponse(e);
                }
                else
                {
                    _subscriptions.TryRemove(this, out SubscriptionMode dummy);
                    throw;
                }
            } 
        }

        private void Notify(JObject newModel)
        {
            lock (_jsonModel)
            {
                _jsonModel = newModel;
            }
            _updateAvailableEvent.Set();
        }

        /// <summary>
        /// Called to notify the subscribers about a model update
        /// </summary>
        /// <returns>Asynchronous task</returns>
        public static async Task Update()
        {
            // This is probably really slow and needs to be improved!
            JObject newModel;
            using (await Model.Provider.AccessReadOnly())
            {
                newModel = JObject.FromObject(Model.Provider.Get, JsonHelper.DefaultSerializer);
            }

            // FIXME cache messages from the updated machine model before we get here and merge them back into newModel.
            // Otherwise two successive model updates without a poll in between would cause messages to disappear  

            if (_subscriptions.Count != 0)
            {
                // Notify subscribers
                foreach (var pair in _subscriptions)
                {
                    pair.Key.Notify(newModel);
                }

                // Clear messages once they have been sent out at least once
                using (await Model.Provider.AccessReadWrite())
                {
                    Model.Provider.Get.Messages.Clear();
                }
            }
        }
    }
}