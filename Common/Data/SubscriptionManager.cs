﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Data
{
    /// <summary>
    /// Enumerable Subscription Management Class
    /// </summary>
    public class SubscriptionManager
    {
        /// Generic Market Data Requested and Object[] Arguements to Get it:
        public List<SubscriptionDataConfig> Subscriptions;

        /// <summary>
        /// Initialise the Generic Data Manager Class
        /// </summary>
        public SubscriptionManager() 
        {
            //Generic Type Data Holder:
            Subscriptions = new List<SubscriptionDataConfig>();
        }

        /// <summary>
        /// Get the count of assets:
        /// </summary>
        public int Count 
        {
            get 
            { 
                return Subscriptions.Count; 
            }
        }

        /// <summary>
        /// Add Market Data Required (Overloaded method for backwards compatibility).
        /// </summary>
        /// <param name="security">Market Data Asset</param>
        /// <param name="symbol">Symbol of the asset we're like</param>
        /// <param name="resolution">Resolution of Asset Required</param>
        /// <param name="fillDataForward">when there is no data pass the last tradebar forward</param>
        /// <param name="extendedMarketHours">Request premarket data as well when true </param>
        public SubscriptionDataConfig Add(SecurityType security, string symbol, Resolution resolution = Resolution.Minute, bool fillDataForward = true, bool extendedMarketHours = false)
        {
            //Set the type: market data only comes in two forms -- ticks(trade by trade) or tradebar(time summaries)
            var dataType = typeof(TradeBar);
            if (resolution == Resolution.Tick) 
            {
                dataType = typeof(Tick);
            }
            return Add(dataType, security, symbol, resolution, fillDataForward, extendedMarketHours, true, true);
        }

        /// <summary>
        /// Add Market Data Required - generic data typing support as long as Type implements IBaseData.
        /// </summary>
        /// <param name="dataType">Set the type of the data we're subscribing to.</param>
        /// <param name="security">Market Data Asset</param>
        /// <param name="symbol">Symbol of the asset we're like</param>
        /// <param name="resolution">Resolution of Asset Required</param>
        /// <param name="fillDataForward">when there is no data pass the last tradebar forward</param>
        /// <param name="extendedMarketHours">Request premarket data as well when true </param>
        /// <param name="isTradeBar">Set to true if this data has Open, High, Low, and Close properties</param>
        /// <param name="hasVolume">Set to true if this data has a Volume property</param>
        /// <param name="isInternalFeed">Set to true to prevent data from this subscription from being sent into the algorithm's OnData events</param>
        public SubscriptionDataConfig Add(Type dataType, SecurityType security, string symbol, Resolution resolution = Resolution.Minute, bool fillDataForward = true, bool extendedMarketHours = false, bool isTradeBar = false, bool hasVolume = false, bool isInternalFeed = false) 
        {
            //Clean:
            symbol = symbol.ToUpper();
            //Create:
            var newConfig = new SubscriptionDataConfig(dataType, security, symbol, resolution, fillDataForward, extendedMarketHours, isTradeBar, hasVolume, isInternalFeed, Subscriptions.Count);

            //For now choose the liquidity and country codes based on our current data providers.
            // This gives us room to grow to international markets and other brokerage providers.
            switch (security)
            {
                case SecurityType.Forex:
                    //Currently QC FX data source is FXCM pricing.
                    newConfig.LiquditySource = LiquiditityProviderDataSource.FXCM;
                    break;
                case SecurityType.Equity:
                    //Currently QC Equities are US Only.
                    newConfig.Country = CountryCode.USA;
                    break;
            }

            //Add to subscription list: make sure we don't have his symbol:
            Subscriptions.Add(newConfig);

            return newConfig;
        }

        /// <summary>
        /// Add a consolidator for the symbol
        /// </summary>
        /// <param name="symbol">Symbol of the asset to consolidate</param>
        /// <param name="consolidator">The consolidator</param>
        public void AddConsolidator(string symbol, IDataConsolidator consolidator)
        {
            symbol = symbol.ToUpper();

            //Find the right subscription and add the consolidator to it
            for (var i = 0; i < Subscriptions.Count; i++)
            {
                if (Subscriptions[i].Symbol == symbol)
                {
                    // we need to be able to pipe data directly from the data feed into the consolidator
                    if (!consolidator.InputType.IsAssignableFrom(Subscriptions[i].Type))
                    {
                        throw new ArgumentException(string.Format("Type mismatch found between consolidator and symbol. " +
                            "Symbol: {0} expects type {1} but tried to register consolidator with input type {2}", 
                            symbol, Subscriptions[i].Type.Name, consolidator.InputType.Name)
                            );
                    }
                    Subscriptions[i].Consolidators.Add(consolidator);
                    return;
                }
            }

            //If we made it here it is because we never found the symbol in the subscription list
            throw new ArgumentException("Please subscribe to this symbol before adding a consolidator for it. Symbol: " + symbol);
        }

    } // End Algorithm MetaData Manager Class

} // End QC Namespace
