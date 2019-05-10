﻿using System;

namespace DuetAPI
{
    /// <summary>
    /// Holds information about a parsed G-code file
    /// </summary>
    public class ParsedFileInfo : ICloneable
    {
        /// <summary>
        /// The filename of the G-code file
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// Size of the file
        /// </summary>
        public long Size { get; set; }
        
        /// <summary>
        /// Date and time of the last modification or null if none is set 
        /// </summary>
        public DateTime? LastModified { get; set; }
        
        /// <summary>
        /// Build height of the G-code job or 0 if not found (in mm)
        /// </summary>
        public float Height { get; set; }
        
        /// <summary>
        /// Height of the first layer or 0 if not found (in mm)
        /// </summary>
        public float FirstLayerHeight { get; set; }
        
        /// <summary>
        /// Height of each other layer or 0 if not found (in mm)
        /// </summary>
        public float LayerHeight { get; set; }
        
        /// <summary>
        /// Number of total layers or null if unknown
        /// </summary>
        public int? NumLayers { get; set; }
        
        /// <summary>
        /// Filament consumption per extruder drive (in mm)
        /// </summary>
        public float[] Filament { get; set; } = new float[0];

        /// <summary>
        /// Name of the application that generated this file
        /// </summary>
        public string GeneratedBy { get; set; } = "";
        
        /// <summary>
        /// Estimated print time (in s)
        /// </summary>
        public long PrintTime { get; set; }
        
        /// <summary>
        /// Estimated print time from G-code simulation (in s)
        /// </summary>
        public long SimulatedTime { get; set; }

        /// <summary>
        /// Creates a clone of this instance
        /// </summary>
        /// <returns>A clone of this instance</returns>
        public object Clone()
        {
            return new ParsedFileInfo
            {
                FileName = (FileName != null) ? string.Copy(FileName) : null,
                Size = Size,
                LastModified = LastModified,
                Height = Height,
                FirstLayerHeight = FirstLayerHeight,
                LayerHeight = LayerHeight,
                NumLayers = NumLayers,
                Filament = (float[])Filament.Clone(),
                GeneratedBy = string.Copy(GeneratedBy),
                PrintTime = PrintTime,
                SimulatedTime = SimulatedTime
            };
        }
    }
}