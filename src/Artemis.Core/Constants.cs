﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Interop;
using Artemis.Core.Plugins.Models;

namespace Artemis.Core
{
    public static class Constants
    {
        /// <summary>
        ///     The full path to the Artemis application folder
        /// </summary>
        public static readonly string ApplicationFolder = Path.GetDirectoryName(typeof(Constants).Assembly.Location);

        /// <summary>
        ///     The full path to the Artemis data folder
        /// </summary>
        public static readonly string DataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\Artemis\\";

        /// <summary>
        ///     The connection string used to connect to the database
        /// </summary>
        public static readonly string ConnectionString = $"FileName={DataFolder}\\database.db";

        /// <summary>
        ///     The plugin info used by core components of Artemis
        /// </summary>
        public static readonly PluginInfo CorePluginInfo = new PluginInfo {Guid = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), Name = "Artemis Core"};

        /// <summary>
        ///     A read-only collection containing all primitive numeric types
        /// </summary>
        public static IReadOnlyCollection<Type> NumberTypes = new List<Type>
        {
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        /// <summary>
        ///     A read-only collection containing all primitive integral numeric types
        /// </summary>
        public static IReadOnlyCollection<Type> IntegralNumberTypes = new List<Type>
        {
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong)
        };

        /// <summary>
        ///     A read-only collection containing all primitive floating-point numeric types
        /// </summary>
        public static IReadOnlyCollection<Type> FloatNumberTypes = new List<Type>
        {
            typeof(float),
            typeof(double),
            typeof(decimal) 
        };
    }
}