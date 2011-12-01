﻿using System.Windows;
//******************************************************************************************************
//  OutputStreamDeviceDigitalUserControl.cs - Gbtc
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
//  09/14/2011 - Aniket Salver
//       Generated original version of source code.
//
//******************************************************************************************************
using System.Windows.Controls;
using System.Windows.Input;
using openPDC.UI.ViewModels;

namespace openPDC.UI.UserControls
{
    /// <summary>
    /// Interaction logic for OutputStreamDeviceDigitalUserControl.xaml
    /// </summary>
    public partial class OutputStreamDeviceDigitalUserControl : UserControl
    {
        #region [ Members ]

        private OutputStreamDeviceDigitals m_dataContext;

        #endregion

        #region[Constructor]

        /// <summary>
        /// Creates an instance of <see cref="OutputStreamDeviceDigitalUserControl"/>
        /// </summary>
        public OutputStreamDeviceDigitalUserControl(int outputStreamDeviceID)
        {
            InitializeComponent();
            m_dataContext = new OutputStreamDeviceDigitals(outputStreamDeviceID, 20, true);
            this.DataContext = m_dataContext;
        }

        #endregion

        #region[Methods]

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid.SelectedItems.Count > 0)
                {
                    if (MessageBox.Show("Are you sure you want to delete " + dataGrid.SelectedItems.Count + " selected item(s)?", "Delete Selected Items", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        e.Handled = true;
                }
            }
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            m_dataContext.SortData(e.Column.SortMemberPath);
        }

        private void GridDetailView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (m_dataContext.IsNewRecord)
                DataGridList.SelectedIndex = -1;
        }

        #endregion
    }
}
