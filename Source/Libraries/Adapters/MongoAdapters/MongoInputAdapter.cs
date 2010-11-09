﻿//******************************************************************************************************
//  MongoInputAdapter.cs - Gbtc
//
//  Copyright © 2010, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  11/05/2010 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using MongoDB;
using TimeSeriesFramework;
using TimeSeriesFramework.Adapters;

namespace MongoAdapters
{
    /// <summary>
    /// Represents an input adapter that reads measurements from a MongoDB database.
    /// </summary>
    public class MongoInputAdapter : InputAdapterBase
    {

        #region [ Members ]

        // Fields

        private string m_databaseName;
        private string m_collectionName;
        private string m_server;
        private int m_port;
        private int m_framesPerSecond;
        private bool m_simulateRealtime;

        private Timer m_timer;
        private Mongo m_mongo;
        private IMongoDatabase m_measurementDatabase;
        private IMongoCollection<MeasurementWrapper> m_measurementCollection;
        private long m_lastTimestamp;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="MongoInputAdapter"/> class.
        /// </summary>
        public MongoInputAdapter()
        {
            m_databaseName = "openPDC";
            m_collectionName = "measurements";
            m_server = "localhost";
            m_port = 27017;
            m_framesPerSecond = 30;
            m_simulateRealtime = true;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the name of the MongoDB database.
        /// </summary>
        public string DatabaseName
        {
            get
            {
                return m_databaseName;
            }
            set
            {
                if (Enabled)
                {
                    string errorMessage = "Cannot modify database name while MongoOutputAdapter is running.";
                    throw new InvalidOperationException(errorMessage);
                }

                m_databaseName = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the measurement collection.
        /// </summary>
        public string CollectionName
        {
            get
            {
                return m_collectionName;
            }
            set
            {
                if (Enabled)
                {
                    string errorMessage = "Cannot modify collection name while MongoOutputAdapter is running.";
                    throw new InvalidOperationException(errorMessage);
                }

                m_collectionName = value;
            }
        }

        /// <summary>
        /// Gets or sets the server on which the MongoDB daemon is running.
        /// </summary>
        public string Server
        {
            get
            {
                return m_server;
            }
            set
            {
                if (Enabled)
                {
                    string errorMessage = "Cannot modify server while MongoInputAdapter is running.";
                    throw new InvalidOperationException(errorMessage);
                }

                m_server = value;
            }
        }

        /// <summary>
        /// Gets or sets the port on which the MongoDB daemon is listening.
        /// </summary>
        public int Port
        {
            get
            {
                return m_port;
            }
            set
            {
                if (Enabled)
                {
                    string errorMessage = "Cannot modify the port while MongoInputAdapter is running.";
                    throw new InvalidOperationException(errorMessage);
                }

                m_port = value;
            }
        }

        /// <summary>
        /// Returns false; this adapter connects to MongoDB synchronously.
        /// </summary>
        protected override bool UseAsyncConnect
        {
            get { return false; }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Initializes the <see cref="MongoInputAdapter"/> using settings from the connection string.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            Dictionary<string, string> settings = Settings;
            string setting;

            // Optional settings

            if (settings.TryGetValue("databaseName", out setting))
                m_databaseName = setting;

            if (settings.TryGetValue("collectionName", out setting))
                m_collectionName = setting;

            if (settings.TryGetValue("server", out setting))
                m_server = setting;

            if (settings.TryGetValue("port", out setting))
                m_port = int.Parse(setting);

            if (settings.TryGetValue("framesPerSecond", out setting))
                m_framesPerSecond = int.Parse(setting);

            if (settings.TryGetValue("simulateRealtime", out setting))
                m_simulateRealtime = Convert.ToBoolean(setting);
        }

        /// <summary>
        /// Attempts to connect to MongoDB.
        /// </summary>
        protected override void AttemptConnection()
        {
            string connectionString = string.Format("server={0}; port={1}", m_server, m_port);

            // Connect to the MongoDB daemon.
            m_mongo = new Mongo(connectionString);
            m_mongo.Connect();

            // Retrieve the database, collection, and measurement timestamps.
            m_measurementDatabase = m_mongo.GetDatabase(m_databaseName);
            m_measurementCollection = m_measurementDatabase.GetCollection<MeasurementWrapper>(m_collectionName);

            // Begin the timer to publish the measurements.
            m_timer = new Timer(1000.0 / m_framesPerSecond);
            m_timer.Elapsed += Timer_Elapsed;
            m_timer.AutoReset = true;
            m_timer.Start();
        }

        /// <summary>
        /// Attempts to disconnect from MongoDB.
        /// </summary>
        protected override void AttemptDisconnection()
        {
            m_mongo.Disconnect();
        }

        /// <summary>
        /// Returns a short message describing the current status of the adapter.
        /// </summary>
        /// <param name="maxLength">The maximum length of the status message.</param>
        /// <returns>The short status message.</returns>
        public override string GetShortStatus(int maxLength)
        {
            return string.Format("Measurements retrieved: {0}", ProcessedMeasurements);
        }

        // The timer that publishes measurements coming from the MongoDB database.
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                long nowTicks = DateTime.UtcNow.Ticks;
                ICursor<MeasurementWrapper> wrappers;
                List<IMeasurement> measurements;
                long minTimestamp;

                // Find all measurements whose timestamp is larger than the last-used timestamp.
                wrappers = m_measurementCollection.Find(new { Timestamp = Op.GreaterThan(m_lastTimestamp) });

                // If no measurements could be found, use all the measurements.
                if (wrappers.Documents.Count() < 1)
                    wrappers = m_measurementCollection.FindAll();

                // Find the smallest timestamp that is also larger than the last-used timestamp.
                minTimestamp = wrappers.Documents.Select(wrapper => wrapper.Timestamp).Min();

                // Find all measurements with the timestamp that was found.
                wrappers = m_measurementCollection.Find(new { Timestamp = minTimestamp });
                measurements = wrappers.Documents.Select(wrapper => wrapper.GetMeasurement()).ToList();

                // Simulate real-time.
                if (m_simulateRealtime)
                    measurements.ForEach(measurement => measurement.Timestamp = nowTicks);

                // Set the last-used timestamp to the new value and publish the measurements.
                m_lastTimestamp = minTimestamp;
                OnNewMeasurements(measurements);
            }
            catch
            {
                // Stop the timer to prevent flooding
                // the user with error messages.
                m_timer.Stop();
                throw;
            }
        }

        #endregion
    }
}
