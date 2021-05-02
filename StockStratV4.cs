#region Using declarations
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using System.ComponentModel.Design;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class StockStratV4 : Strategy
	{
        #region Variables
        #region Indicators
        private EMA iEMA1, iEMA2, iEMA3;
		private ATR iATR;
		private VOL iVOL;
		private VOLMA iVOLMA;
		private Swing iSwing1, iSwing2;
		#endregion

		#region Parameters_Check
		private int max_indicator_bar_calculation = 0;
		#endregion

		#region Overall_Market_Movement
		private int cross_above_bar, cross_below_bar;
		private double ATR_crossing_value;
		private bool is_upward, is_downward;
		#endregion

		private int AmountLong, FixAmountLong, AmountShort, FixAmountShort;
		private double EMA_distance, StopPriceLong, TriggerPriceLong, StopPriceShort, TriggerPriceShort, swingHigh2_max, SwingLow14min, MaxCloseSwingLow4, RangeSizeSwingLow4, MaxCloseSwingLow14;
		private double last_max_high_swingHigh1, last_max_high_swingHigh2, last_min_low_swingLow1, last_min_low_swingLow2, ExtremeClose, FixDistanceToPullbackClose, FixStopSize, StopSize;
		private double RangeSizeSwingLow14, MinCloseSwingHigh4, RangeSizeSwingHigh4, MinCloseSwingHigh14, RangeSizeSwingHigh14, swingHigh2_maxReentry, SwingLow14minReentry, max_high_swingHigh1, max_high_swingHigh2, min_low_swingLow1, min_low_swingLow2;
		private double last_swingHigh2, last_swingLow2, last_swingHigh1, last_swingLow1, DistanceToSMA, DistanceToPullbackClose, FixStopPriceLong, FixTriggerPriceLong, FixStopPriceShort, FixTriggerPriceShort;
		private bool is_incipient_up_trend, is_incipient_down_trend, gray_ellipse_long, gray_ellipse_short, isReentryLong, isReentryShort, isLong, isShort, isBO, is_BO_up_swing1, is_BO_up_swing2, is_BO_down_swing1, is_BO_down_swing2;			
		private bool cross_below_iSMA3_to_iSMA2, cross_above_iSMA3_to_iSMA2, isTrailing;
		private Account myAccount;
		private Order myEntryOrder = null, myEntryMarket = null, myExitOrder = null;
        #endregion

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"no need of grayellipse, just an approach between EMA 20 and 50";
				Name										= "StockStratV5";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= false;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= true;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				IsInstantiatedOnEachOptimizationIteration	= true;

				EMA1						= 100;
				EMA2						= 25;
				EMA3						= 10;
				ATR1						= 20;
				Swing1						= 3;
				Swing2						= 9;
				UnitsTriggerForTrailing		= 1;				
				TrailingUnitsStop			= 2;
				RiskUnit                    = 500;	
				IncipientTrendFactor		= 2;
				ATRSMAFactor				= 3;
				ATRPullbackFactor			= 4;
				ATRStopFactor				= 2;
				ATR_EMA_cross_factor			= 2;
				EMAForTrailing				= 2;
				EMAShield					= 2;
				TicksToBO					= 10;
				SwingPenetration			= 300;
				ClosnessFactor				= 2;
				ClosnessToTrade				= 0.1;
				MagicNumber					= 100;
				VolumenPercent				= -100;
				HLorLH						= 0;
			}
			else if (State == State.DataLoaded)
			{
				// iEMA1 > iEMA2 > iEMA3
				iEMA1			= EMA(Close, EMA1);
				iEMA2			= EMA(Close, EMA2);
				iEMA3			= EMA(Close, EMA3);

				iATR			= ATR(Close, ATR1);
				iVOL			= VOL();
				iVOLMA			= VOLMA(10);
				iSwing1			= Swing(Close, Swing1);
				iSwing2			= Swing(Close, Swing2);

				iEMA1.Plots[0].Brush= Brushes.Red;
				iEMA2.Plots[0].Brush = Brushes.Gold;
				iEMA3.Plots[0].Brush = Brushes.Lime;
				iATR.Plots[0].Brush = Brushes.White;
				iSwing1.Plots[0].Brush = Brushes.Fuchsia;
				iSwing1.Plots[1].Brush = Brushes.Fuchsia;
				iSwing2.Plots[0].Brush = Brushes.Silver;
				iSwing2.Plots[1].Brush = Brushes.Silver;

				AddChartIndicator(iEMA1);
				AddChartIndicator(iEMA2);
				AddChartIndicator(iEMA3);
				AddChartIndicator(iATR);
				AddChartIndicator(iSwing1);
				AddChartIndicator(iSwing2);
//				AddChartIndicator(iVOL);
//				AddChartIndicator(iVOLMA);
			}
		}
		
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled,
			double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{
			  // Assign entryOrder in OnOrderUpdate() to ensure the assignment occurs when expected.
			  // This is more reliable than assigning Order objects in OnBarUpdate, as the assignment is not gauranteed to be complete if it is referenced immediately after submitting
			  if (order.Name == "entryOrder")
			  {
			      myEntryOrder = order;
			  }
			  if (order.Name == "entryMarket")
			  {
			      myEntryMarket = order;
			  }
			  if (order.Name == "exit")
			  {
			      myExitOrder = order;
			  }
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            // Reset our stop order and target orders' Order objects after our position is closed. (1st Entry)
            if (myExitOrder != null)
            {
				if (myExitOrder == execution.Order)
				{
	                if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled)
	                {
						if (myEntryOrder != null)
						{
	                    	myEntryOrder.OrderState = OrderState.Cancelled;
						}

						if (myEntryMarket != null)
						{
							myEntryMarket.OrderState = OrderState.Cancelled;
						}
	                }
				}
            }
			if (myEntryMarket != null)
            {
				if (myEntryMarket == execution.Order)
				{
	                if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled)
	                {
						if (myEntryOrder != null)
						{
							if (myEntryOrder.OrderState == OrderState.Filled)
							{
	                    		myEntryOrder.OrderState = OrderState.Cancelled;
							}
						}							
	                }
				}
            }
        }
		
		protected override void OnBarUpdate()
		{
			#region Chart_Initialization
			if (BarsInProgress != 0) return;

			if (CurrentBars[0] < 0) return;

			if (CurrentBar < BarsRequiredToTrade || CurrentBar < max_indicator_bar_calculation) return;

			//This conditional checks that the indicator values that will be used in later calculations are not equal to 0.
			if (iSwing1.SwingHigh[0] == 0 || iSwing1.SwingLow[0] == 0 ||
				iSwing2.SwingHigh[0] == 0 || iSwing2.SwingLow[0] == 0 ||
				iEMA1[0] == 0 ||
				iATR[0] == 0)
				return;
			#endregion

			#region Variable_Reset

			//Reset is_BO variable once a new swing comes up.
			#region BO_Reset
			if (last_swingHigh1 != iSwing1.SwingHigh[0])
				is_BO_up_swing1 = false;

			if (last_swingHigh2 != iSwing2.SwingHigh[0])
				is_BO_up_swing2 = false;

			if (last_swingLow1 != iSwing1.SwingLow[0])
				is_BO_down_swing1 = false;

			if (last_swingLow2 != iSwing2.SwingLow[0])
				is_BO_down_swing2 = false;
			#endregion

			last_max_high_swingHigh1 = max_high_swingHigh1;
			last_max_high_swingHigh2 = max_high_swingHigh2;
			last_min_low_swingLow1 = min_low_swingLow1;
			last_min_low_swingLow2 = min_low_swingLow2;

            //Variable setting for recurrent data sources.
            last_swingHigh1 = iSwing1.SwingHigh[0];
			last_swingLow1 = iSwing1.SwingLow[0];
			last_swingHigh2 = iSwing2.SwingHigh[0];
			last_swingLow2 = iSwing2.SwingLow[0];
			//			DistanceToSMA = iATR[0] * ATRSMAFactor;
			//			DistanceToPullbackClose = iATR[0] * ATRPullbackFactor;

			//If there isn't an active position,
			//the trend flags (is_long, is_short) are reset to false
			//for later calculations.
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				isLong = false;
				isShort = false;
				isTrailing = false;
				StopSize = iATR[0] * ATRStopFactor;
				DistanceToSMA = iATR[0] * ATRSMAFactor;
				DistanceToPullbackClose = iATR[0] * ATRPullbackFactor;
			}

			//Save the distance between the 2 biggest EMAs.	
			EMA_distance = Math.Abs(iEMA1[0] - iEMA2[0]);
			#endregion

			#region Parameters_Check
			//This block of code checks if the indicator values that will be used in later calculations are correct.
			//When a SMA of period 200 prints a value in the bar 100 is an example of a wrong indicator value.
			//This conditional only executes once, that is why its argument.
			if (max_indicator_bar_calculation == 0)
			{
				//Create an array so that we can get the maximum value.
				int[] indicators = { EMA1, EMA2, EMA3, ATR1 };

				max_indicator_bar_calculation = indicators.Max();

				//Stop calculations if the chart is not printing correct indicator values.
				if (CurrentBar < max_indicator_bar_calculation)
				{
					Print(string.Format("BarsRequiredToTrade has been updated to {0} to avoid wrong indicator values.", max_indicator_bar_calculation));
					return;
				}
			}

			//Make sure that the Trailing_Stop won't be set below/above the maximum/minimum stop point. 
			//This by setting the UnitsTriggerForTrailing by 1 below the TrailingUnitsStop if necessary.
			if (TrailingUnitsStop - UnitsTriggerForTrailing > 1)
			{
				UnitsTriggerForTrailing = TrailingUnitsStop - 1;
				Print(string.Format("UnitsTriggerForTrailing has been set to {0} to avoid stop conflicts.", UnitsTriggerForTrailing));
			}
			#endregion

			#region Overall_Market_Movement
			//If the second biggest SMA crosses above the biggest SMA it means the market is going upwards.
			//The ATR value is saved immediately, the is_upward flag is set to true while its opposite flag (is_downward) is set to false.
			//Finally the CurrentBar of the event is saved for later calculations.
			if (CrossAbove(iEMA2, iEMA1, 1))
			{
				ATR_crossing_value = iATR[0];
				is_upward = true;
				is_downward = false;
				cross_above_bar = CurrentBar;
			}

			//If the second biggest SMA crosses below the biggest SMA it means the market is going downwards.
			//The ATR value is saved immediately, the is_downward flag is set to true while its opposite flag (is_upward) is set to false.
			//Finally the CurrentBar of the event is saved for later calculations.
			if (CrossBelow(iEMA2, iEMA1, 1))
			{
				ATR_crossing_value = iATR[0];
				is_downward = true;
				is_upward = false;
				cross_below_bar = CurrentBar;
			}
			#endregion

			#region Incipient_Trend_Identification
			//IncipientTrend: There has been a crossing event with
			//the 2 biggest SMAs and the distance between them is
			//greater or equal to the ATR value at the SMAs crossing
			//event multiplied by the IncipientTrendFactor parameter.

			//If the overall market movement is going upwards and
			//the Incipient Trend is confirmed, the is_incipient_up_trend
			//flag is set to true while its opposite
			//(is_incipient_down_trend) is set to false and the
			//gray_ellipse_short flag is set to false.
			if (is_upward && 
				EMA_distance >= IncipientTrendFactor * ATR_crossing_value)
			{
				is_incipient_up_trend = true;
				is_incipient_down_trend = false;
				gray_ellipse_short = false;
				cross_below_iSMA3_to_iSMA2 = false;
			}

			//If the overall market movement is going downwards and
			//the Incipient Trend is confirmed, the
			//is_incipient_down_trend flag is set to true while its
			//opposite (is_incipient_up_trend) is set to false and the
			//gray_ellipse_long flag is set to false.
			if (is_downward && 
				EMA_distance >= IncipientTrendFactor * ATR_crossing_value) //Once an downward overall movement has been identified, the SMAs 50-200 distance is greater than the ATR value at the crossing time and there have been a first CrossBelow 50-200 event that records an ATR value then...
			{
				is_incipient_down_trend = true;
				is_incipient_up_trend = false;
				gray_ellipse_long = false;
				cross_above_iSMA3_to_iSMA2 = false;
			}
			#endregion

			#region Gray Ellipse
			//Gray Ellipse: Is when the smallest and second biggest
			//EMAs crosses in the opposite direction of the overall
			//market movement.

			//This if statement recognizes the cross below event
			//between the smallest and second biggest SMAs nevertheless,
			//it does not mean that a gray ellipse event has happened.
			if (Math.Abs(iEMA3[0] - iEMA2[0]) <= iATR[0] * ATR_EMA_cross_factor)
			{
			 	cross_below_iSMA3_to_iSMA2 = true;
			}

			//If both events (Incipient Trend event and smallest SMAs
			//opposite cross) happens it means that a Gray Ellipse
			//event has happened.
			//That is why the gray_ellipse_long flag is set to true
			//while the cross_below_iSMA3_to_iSMA2 is reset to false
			//for next events.
			//Then there is going to be an iteration in order to find
			//the highest swingHigh2 between the biggest SMA cross
			//event and the gray ellipse.
			if (is_incipient_up_trend && cross_below_iSMA3_to_iSMA2)
			{
				cross_below_iSMA3_to_iSMA2 = false;
				isReentryLong = false;
				gray_ellipse_long = true;

				//This if statement is done to avoid the bug when
				//trying to find the value of swing indicator beyond
				//the 256 MaximunBarLookBack period, which is not
				//possible.
				int SMAs_cross_and_gray_ellipse_dis = CurrentBar - cross_above_bar; //Calculating the distance in bars (that determines the array size to check for the max/min swinghigh/low level) between the SMAs 20-50 crossing event and the Last SMAs 50-200 crossing event			
				if (SMAs_cross_and_gray_ellipse_dis > 240)
				{
					SMAs_cross_and_gray_ellipse_dis = 240;	
				}

				//Iterate to find the highest swingHigh2
				swingHigh2_max = iSwing2.SwingHigh[0];
				for (int i = 0; i <= SMAs_cross_and_gray_ellipse_dis; i++)
            	{
					if (iSwing2.SwingHigh[i] > swingHigh2_max)
                	{
						swingHigh2_max = iSwing2.SwingHigh[i];
					}
				}
			}
			//If the swingHigh of the current bar surpass the
			//Max Level of the swingHigh (bigggest swing) where the
			//gray_ellipse_long was originated, and the current swing
			//is right next to the gray_ellipse swing it means that a
			//Reentry trade can be executed.
			//That is why the is_reentry_long flag is set to true and
			//the current swingHigh (biggest swing) is saved to keep
			//track of the last swing where a Reentry trade can be
			//executed.
			else if (gray_ellipse_long &&
				iSwing2.SwingHigh[0] > swingHigh2_max)
			{
				swingHigh2_maxReentry = iSwing2.SwingHigh[0];
				isReentryLong = true;
			}

			//If the current high surpass the swingHigh where a
			//Reentry trade can be executed, it means that this trade
			//type can't be done again before another gray_ellipse.
			//That is why the gray_ellipse_long flag and the
			//is_reentry_long flag are set to false.
			if (isReentryLong &&
				High[0] > swingHigh2_maxReentry)
			{
				gray_ellipse_long = false;
				isReentryLong = false;
			}
            #endregion

            ////		TRADE IDENTIFICATION			
            ////		Red Trade Type Process			
            ///Long			 
            if (is_incipient_down_trend && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)) //if the overall market movement is upward and there is no active position then...
			{
				///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4				
				bool isSwingHigh4 = false; //Higher Swing strength 4 Flag creation, set to false and pending of validation
				bool isActiveLongPosition = false;
				if (iSwing1.SwingHigh[0] >= iEMA1[0] - iATR[0] * ClosnessToTrade && ((iSwing1.SwingHigh[0] + TicksToBO * TickSize) - iEMA1[0] <= DistanceToSMA || (iSwing1.SwingHigh[0] + TicksToBO * TickSize) - iEMA2[0] <= DistanceToSMA)) // If the reference Swing High 4 is above the SMA 50 then...
				{			
					Tuple<bool, bool, bool> ReturnedValues = Swing4(iEMA1, "High");
					isSwingHigh4 = ReturnedValues.Item1;
					isActiveLongPosition = ReturnedValues.Item2;
					is_BO_up_swing1 = ReturnedValues.Item3;
				}				
				
				///Validate whether there is a valid higher low strength 14 (HL14) in case there is no valid HL4 and if so then send a long stop order above the reference swing high 14				
				if (iSwing2.SwingHigh[0] != iSwing1.SwingHigh[0] && iSwing2.SwingHigh[0] >= iEMA1[0] - iATR[0] * ClosnessToTrade && !isSwingHigh4 && !isActiveLongPosition && ((iSwing2.SwingHigh[0] + TicksToBO * TickSize) - iEMA1[0] <= DistanceToSMA || (iSwing2.SwingHigh[0] + TicksToBO * TickSize) - iEMA2[0] <= DistanceToSMA)) // If the reference Swing High 14 is above the SMA 50 and there is no HL4 then...
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing14(iEMA1, "High");
					bool isSwingHigh14 = ReturnedValues.Item1;
					isActiveLongPosition = ReturnedValues.Item2;
					is_BO_up_swing2 = ReturnedValues.Item3;
				}				
			}
			 
			///Short		 
			else if (is_incipient_up_trend && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)) //if the overall market movement is upward and there is no active position then...
			{
				///Validate whether there is a valid lower high strength 4 (LH4) and if so then send a short stop order below the reference swing low 4			
				bool isSwingLow4 = false; //Lower Swing strength 4 Flag creation, set to false and pending of validation
				bool isActiveShortPosition = false;
				if (iSwing1.SwingLow[0] <= iEMA1[0] + iATR[0] * ClosnessToTrade && (iEMA1[0] - (iSwing1.SwingLow[0] - TicksToBO * TickSize) <= DistanceToSMA || iEMA2[0] - (iSwing1.SwingLow[0] - TicksToBO * TickSize) <= DistanceToSMA))
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing4(iEMA1, "Low");
					isSwingLow4 = ReturnedValues.Item1;
					isActiveShortPosition = ReturnedValues.Item2;
					is_BO_down_swing1 = ReturnedValues.Item3;
				}
				
				///Validate whether there is a valid lower high strength 14 (LH14) in case there is no valid LH4 and if so then send a short stop order below the reference swing low 14			
				if (iSwing2.SwingLow[0] != iSwing1.SwingLow[0] && iSwing2.SwingLow[0] <= iEMA1[0] + iATR[0] * ClosnessToTrade && !isSwingLow4 && !isActiveShortPosition && (iEMA1[0] - (iSwing2.SwingLow[0] - TicksToBO * TickSize) <= DistanceToSMA || iEMA2[0] - (iSwing2.SwingLow[0] - TicksToBO * TickSize) <= DistanceToSMA))
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing14(iEMA1, "Low");
					bool isSwingLow14 = ReturnedValues.Item1;
					isActiveShortPosition = ReturnedValues.Item2;
					is_BO_down_swing2 = ReturnedValues.Item3;
				}
			}
			
////		TRADITIONAL RED Trade Type Process			
			///Long			 
			if (is_incipient_up_trend && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)) //if the overall market movement is upward and there is no active position then...
			{
				///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4				
				bool isSwingHigh4 = false; //Higher Swing strength 4 Flag creation, set to false and pending of validation
				bool isActiveLongPosition = false;
				if (iSwing1.SwingHigh[0] >= iEMA1[0] - iATR[0] * ClosnessToTrade && (iSwing1.SwingHigh[0] + TicksToBO * TickSize) - iEMA1[0] <= DistanceToSMA && iEMA2[0] - iSwing1.SwingHigh[0] > iATR[0] * ClosnessFactor) // If the reference Swing High 4 is above the SMA 50 then...
				{			
					Tuple<bool, bool, bool> ReturnedValues = Swing4(iEMA1, "High");
					isSwingHigh4 = ReturnedValues.Item1;
					isActiveLongPosition = ReturnedValues.Item2;
					is_BO_up_swing1 = ReturnedValues.Item3;
				}				
				
				///Validate whether there is a valid higher low strength 14 (HL14) in case there is no valid HL4 and if so then send a long stop order above the reference swing high 14				
				if (iSwing2.SwingHigh[0] != iSwing1.SwingHigh[0] && iSwing2.SwingHigh[0] >= iEMA1[0] - iATR[0] * ClosnessToTrade && !isSwingHigh4 && !isActiveLongPosition && (iSwing2.SwingHigh[0] + TicksToBO * TickSize) - iEMA1[0] <= DistanceToSMA && iEMA2[0] - iSwing2.SwingHigh[0] > iATR[0] * ClosnessFactor) // If the reference Swing High 14 is above the SMA 50 and there is no HL4 then...
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing14(iEMA1, "High");
					bool isSwingHigh14 = ReturnedValues.Item1;
					isActiveLongPosition = ReturnedValues.Item2;
					is_BO_up_swing2 = ReturnedValues.Item3;
				}				
			}
			 
			///Short		 
			if (is_incipient_down_trend && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)) //if the overall market movement is upward and there is no active position then...
			{
				///Validate whether there is a valid lower high strength 4 (LH4) and if so then send a short stop order below the reference swing low 4			
				bool isSwingLow4 = false; //Lower Swing strength 4 Flag creation, set to false and pending of validation
				bool isActiveShortPosition = false;
				if (iSwing1.SwingLow[0] <= iEMA1[0] + iATR[0] * ClosnessToTrade && iEMA1[0] - (iSwing1.SwingLow[0] - TicksToBO * TickSize) <= DistanceToSMA && iSwing1.SwingLow[0] - iEMA2[0] > iATR[0] * ClosnessFactor)
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing4(iEMA1, "Low");
					isSwingLow4 = ReturnedValues.Item1;
					isActiveShortPosition = ReturnedValues.Item2;
					is_BO_down_swing1 = ReturnedValues.Item3;
				}
				
				///Validate whether there is a valid lower high strength 14 (LH14) in case there is no valid LH4 and if so then send a short stop order below the reference swing low 14			
				if (iSwing2.SwingLow[0] != iSwing1.SwingLow[0] && iSwing2.SwingLow[0] <= iEMA1[0] + iATR[0] * ClosnessToTrade && !isSwingLow4 && !isActiveShortPosition && iEMA1[0] - (iSwing2.SwingLow[0] - TicksToBO * TickSize) <= DistanceToSMA && iSwing2.SwingLow[0] - iEMA2[0] > iATR[0] * ClosnessFactor)
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing14(iEMA1, "Low");
					bool isSwingLow14 = ReturnedValues.Item1;
					isActiveShortPosition = ReturnedValues.Item2;
					is_BO_down_swing2 = ReturnedValues.Item3;
				}
			}
			
////		Traditional Trade Type Process
			///Long
			if (is_incipient_up_trend && gray_ellipse_long && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)) //if we have an is_incipient_up_trend, a gray_ellipse_long and there is no active position then...
			{			
				///Normal traditional
				if (is_upward)
				{			
					///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4				
					bool isSwingHigh4 = false; //Higher Swing strength 4 Flag creation, set to false and pending of validation
					bool isActiveLongPosition = false;
					if (iSwing1.SwingHigh[0] >= iEMA2[0] - iATR[0] * ClosnessToTrade && (iSwing1.SwingHigh[0] + TicksToBO * TickSize) - iEMA2[0] <= DistanceToSMA) // If the reference Swing High 4 is above the SMA 50 then...
					{	
						Tuple<bool, bool, bool> ReturnedValues = Swing4(iEMA2, "High");
						isSwingHigh4 = ReturnedValues.Item1;
						isActiveLongPosition = ReturnedValues.Item2;
						is_BO_up_swing1 = ReturnedValues.Item3;					
					}					
					
					///Validate whether there is a valid higher low strength 14 (HL14) in case there is no valid HL4 and if so then send a long stop order above the reference swing high 14					
					if (iSwing2.SwingHigh[0] != iSwing1.SwingHigh[0] && iSwing2.SwingHigh[0] >= iEMA2[0] - iATR[0] * ClosnessToTrade && !isSwingHigh4 && !isActiveLongPosition && (iSwing2.SwingHigh[0] + TicksToBO * TickSize) - iEMA2[0] <= DistanceToSMA) // If the reference Swing High 14 is above the SMA 50 and there is no HL4 then...
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing14(iEMA2, "High");
						bool isSwingHigh14 = ReturnedValues.Item1;
						isActiveLongPosition = ReturnedValues.Item2;
						is_BO_up_swing2 = ReturnedValues.Item3;
					}
				}
				///Modified traditional
				else
				{
					///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4			
					bool isSwingHigh4 = false; //Higher Swing strength 4 Flag creation, set to false and pending of validation
					bool isActiveLongPosition = false;
					if (iSwing1.SwingHigh[0] >= iEMA1[0] - iATR[0] * ClosnessToTrade && (iSwing1.SwingHigh[0] + TicksToBO * TickSize) - iEMA1[0] <= DistanceToSMA) // If the reference Swing High 4 is above the SMA 50 then...
					{				
						Tuple<bool, bool, bool> ReturnedValues = Swing4(iEMA1, "High");
						isSwingHigh4 = ReturnedValues.Item1;
						isActiveLongPosition = ReturnedValues.Item2;
						is_BO_up_swing1 = ReturnedValues.Item3;
					}				
					
					///Validate whether there is a valid higher low strength 14 (HL14) in case there is no valid HL4 and if so then send a long stop order above the reference swing high 14				
					if (iSwing2.SwingHigh[0] != iSwing1.SwingHigh[0] && iSwing2.SwingHigh[0] >= iEMA1[0] - iATR[0] * ClosnessToTrade && !isSwingHigh4 && !isActiveLongPosition && (iSwing2.SwingHigh[0] + TicksToBO * TickSize) - iEMA1[0] <= DistanceToSMA) // If the reference Swing High 14 is above the SMA 50 and there is no HL4 then...
					{
						
						Tuple<bool, bool, bool> ReturnedValues = Swing14(iEMA1, "High");
						bool isSwingHigh14 = ReturnedValues.Item1;
						isActiveLongPosition = ReturnedValues.Item2;
						is_BO_up_swing2 = ReturnedValues.Item3;
					}
				}		
			}
			
			///Short	
			else if (is_incipient_down_trend && gray_ellipse_short && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)) //if we have an is_incipient_down_trend, a gray_ellipse_short and there is no active position then...
			{			
				///Normal traditional
				if (is_downward)	
				{				
					///Validate whether there is a valid lower high strength 4 (LH4) and if so then send a short stop order below the reference swing low 4		
					bool isSwingLow4 = false; //Lower Swing strength 4 Flag creation, set to false and pending of validation
					bool isActiveShortPosition = false;
					if (iSwing1.SwingLow[0] <= iEMA2[0] + iATR[0] * ClosnessToTrade && iEMA2[0] - (iSwing1.SwingLow[0] - TicksToBO * TickSize) <= DistanceToSMA)
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing4(iEMA2, "Low");
						isSwingLow4 = ReturnedValues.Item1;
						isActiveShortPosition = ReturnedValues.Item2;
						is_BO_down_swing1 = ReturnedValues.Item3;
					}					
					///Validate whether there is a valid lower high strength 14 (LH14) in case there is no valid LH4 and if so then send a short stop order below the reference swing low 14				
					if (iSwing2.SwingLow[0] != iSwing1.SwingLow[0] && iSwing2.SwingLow[0] <= iEMA2[0] + iATR[0] * ClosnessToTrade && !isSwingLow4 && !isActiveShortPosition && iEMA2[0] - (iSwing2.SwingLow[0] - TicksToBO * TickSize) <= DistanceToSMA)
					{	
						Tuple<bool, bool, bool> ReturnedValues = Swing14(iEMA2, "Low");
						bool isSwingLow14 = ReturnedValues.Item1;
						isActiveShortPosition = ReturnedValues.Item2;
						is_BO_down_swing2 = ReturnedValues.Item3;
					}
				}
				///Modified traditional
				else
				{
					///Validate whether there is a valid lower high strength 4 (LH4) and if so then send a short stop order below the reference swing low 4			
					bool isSwingLow4 = false; //Lower Swing strength 4 Flag creation, set to false and pending of validation
					bool isActiveShortPosition = false;
					if (iSwing1.SwingLow[0] <= iEMA1[0] + iATR[0] * ClosnessToTrade && iEMA1[0] - (iSwing1.SwingLow[0] - TicksToBO * TickSize) <= DistanceToSMA)
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing4(iEMA1, "Low");
						isSwingLow4 = ReturnedValues.Item1;
						isActiveShortPosition = ReturnedValues.Item2;
						is_BO_down_swing1 = ReturnedValues.Item3;
					}				
					///Validate whether there is a valid lower high strength 14 (LH14) in case there is no valid LH4 and if so then send a short stop order below the reference swing low 14				
					if (iSwing2.SwingLow[0] != iSwing1.SwingLow[0] && iSwing2.SwingLow[0] <= iEMA1[0] + iATR[0] * ClosnessToTrade && !isSwingLow4 && !isActiveShortPosition && iEMA1[0] - (iSwing2.SwingLow[0] - TicksToBO * TickSize) <= DistanceToSMA)
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing14(iEMA1, "Low");
						bool isSwingLow14 = ReturnedValues.Item1;
						isActiveShortPosition = ReturnedValues.Item2;
						is_BO_down_swing2 = ReturnedValues.Item3;
					}
				}				
			}
			
////		TRADE MANAGEMENT (Stop and Trailing Stop Trigger Setting)						
			///Stop Updating Process (by both trailing and SMA)
			///While Long	
			if (Position.MarketPosition == MarketPosition.Long && !isLong) //if the postition is stil active and the initial stop and trailing stop trigger price levels were set then...
			{				
				isLong = true;
				isShort = false;
				FixStopSize = StopSize;
				StopPriceLong = Position.AveragePrice - StopSize; //calculates the stop price level
				TriggerPriceLong = Position.AveragePrice + StopSize * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger	
				FixAmountLong = AmountLong;
				FixStopPriceLong = StopPriceLong;
				FixTriggerPriceLong = TriggerPriceLong;	
				FixStopPriceLong = StopPriceLong;
				Draw.FibonacciRetracements(this, "tag1" + CurrentBar, false, 0, Position.AveragePrice, 10, FixStopPriceLong);
			}		
			if (Position.MarketPosition == MarketPosition.Long && isLong) //if the postition is stil active and the initial stop and trailing stop trigger price levels were set then...
			{				
				if (myEntryOrder == null || myEntryMarket == null)
				{
					if (myEntryMarket == null && myEntryOrder.OrderState == OrderState.Filled)
					{
//						if (myEntryOrder.OrderState == OrderState.Filled)
//						{
//							if (myEntryOrder.OrderType == OrderType.StopMarket && myEntryOrder.OrderState == OrderState.Working && myEntryOrder.OrderAction == OrderAction.SellShort)
//							{
//								Print (String.Format("{0} // {1} // {2} // {3}", myEntryOrder.StopPrice, FixStopPriceLong,  myEntryOrder.Quantity, Time[0]));
//							}
//							else
//							{
								ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
//							}
//						}										
						if (High[BarsSinceEntryExecution(0, @"entryOrder", 0)] < iEMA2[BarsSinceEntryExecution(0, @"entryOrder", 0)] + TicksToBO * TickSize)
						{
							bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i=0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
							{
								if (High[i] >= iEMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
					        		HighCrossEma50 = true; //And saves that value in this variable
									break;
								}								
							}								
							if (HighCrossEma50 == false)
							{
								if ((Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryOrder");
								}
							}
							else
							{
								if ((Close[0] < iEMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryOrder");
								}
							}
						}
						else
						{
							if ((Close[0] < iEMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(FixAmountLong, @"exit", @"entryOrder");
							}
						}
						if (High[0] >= FixTriggerPriceLong)
						{
							FixStopPriceLong = High[0] - FixStopSize * TrailingUnitsStop;
							FixTriggerPriceLong = High[0];
							ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
							isTrailing = true;
							Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
						}
						if (isTrailing && iEMA2[0] - iATR[0] > FixStopPriceLong)
						{
							FixStopPriceLong = iEMA2[0] - iATR[0] * EMAShield;
							ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
						}
					}
					else if (myEntryOrder == null && myEntryMarket.OrderState == OrderState.Filled)
					{
						ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryMarket");
	//						}
	//					}				
						if (High[BarsSinceEntryExecution(0, @"entryMarket", 0)] < iEMA2[BarsSinceEntryExecution(0, @"entryMarket", 0)] + TicksToBO * TickSize)
						{
							bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i=0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
							{
								if (High[i] >= iEMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
					        		HighCrossEma50 = true; //And saves that value in this variable
									break;
								}								
							}								
							if (HighCrossEma50 == false)
							{
								if ((Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryMarket");
								}
							}
							else
							{
								switch(EMAForTrailing)
								  {                        
								    case 1:
										if ((Close[0] < iEMA3[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
										{
											Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
											ExitLong(FixAmountLong, @"exit", @"entryMarket");
										}
								        break;        
								    case 2:
										if ((Close[0] < iEMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
										{
											Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
											ExitLong(FixAmountLong, @"exit", @"entryMarket");
										}
								        break;							       																
								  }
							}
						}
						else
						{
							switch(EMAForTrailing)
							  {                        
							    case 1:
									if ((Close[0] < iEMA3[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
									{
										Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
										ExitLong(FixAmountLong, @"exit", @"entryMarket");
									}
							        break;        
							    case 2:
									if ((Close[0] < iEMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
									{
										Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
										ExitLong(FixAmountLong, @"exit", @"entryMarket");
									}
							        break;							       																
							  }
								
						}
						if (High[0] >= FixTriggerPriceLong)
						{
							FixStopPriceLong = High[0] - FixStopSize * TrailingUnitsStop;
							FixTriggerPriceLong = High[0];
							ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryMarket");
							isTrailing = true;
							Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
						}
						if (isTrailing && iEMA2[0] - iATR[0] > FixStopPriceLong)
						{
							FixStopPriceLong = iEMA2[0] - iATR[0] * EMAShield;
							ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
						}
					}
				}
				else if (myEntryOrder.OrderState == OrderState.Filled/* && myEntryMarket.OrderState != OrderState.Filled*/)
				{
//					if (myEntryOrder.OrderState == OrderState.Filled && myEntryMarket.OrderState != OrderState.Filled)
//					{
//						if (myEntryOrder.OrderType == OrderType.StopMarket && myEntryOrder.OrderState == OrderState.Working && myEntryOrder.OrderAction == OrderAction.SellShort)
//						{
//							Print (String.Format("{0} // {1} // {2} // {3}", myEntryOrder.StopPrice, FixStopPriceLong,  myEntryOrder.Quantity, Time[0]));
//						}
//						else
//						{
							ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
//						}
//					}				
					if (High[BarsSinceEntryExecution(0, @"entryOrder", 0)] < iEMA2[BarsSinceEntryExecution(0, @"entryOrder", 0)] + TicksToBO * TickSize)
						{
							bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i=0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
							{
								if (High[i] >= iEMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
					        		HighCrossEma50 = true; //And saves that value in this variable
									break;
								}								
							}								
							if (HighCrossEma50 == false)
							{
								if ((Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryOrder");
								}
							}
							else
							{
								if ((Close[0] < iEMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryOrder");
								}
							}
						}
						else
						{
							if ((Close[0] < iEMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(FixAmountLong, @"exit", @"entryOrder");
							}
						}
					if (High[0] >= FixTriggerPriceLong)
					{
						FixStopPriceLong = High[0] - FixStopSize * TrailingUnitsStop;
						FixTriggerPriceLong = High[0];
						ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
						isTrailing = true;
						Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
					}
					if (isTrailing && iEMA2[0] - iATR[0] > FixStopPriceLong)
					{
						FixStopPriceLong = iEMA2[0] - iATR[0] * EMAShield;
						ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
					}
				}
				else if (myEntryMarket.OrderState == OrderState.Filled/* && myEntryOrder.OrderState != OrderState.Filled*/)
				{
//					if (myEntryMarket.OrderState == OrderState.Filled && myEntryOrder.OrderState != OrderState.Filled)
//					{
						if (myEntryOrder.OrderType == OrderType.StopMarket && myEntryOrder.OrderState == OrderState.Working && myEntryOrder.OrderAction == OrderAction.SellShort)
						{
//							Print (String.Format("{0} // {1} // {2} // {3}", myEntryOrder.StopPrice, FixStopPriceLong,  myEntryMarket.Quantity, Time[0]));
						}
						else
						{
							ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryMarket");
						}
//					}				
					if (High[BarsSinceEntryExecution(0, @"entryMarket", 0)] < iEMA2[BarsSinceEntryExecution(0, @"entryMarket", 0)] + TicksToBO * TickSize)
					{
						bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i=0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
						{
							if (High[i] >= iEMA2[i]) //To determine the highest value (max close value of the swinglow4)
							{
				        		HighCrossEma50 = true; //And saves that value in this variable
								break;
							}								
						}								
						if (HighCrossEma50 == false)
						{
							if ((Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(FixAmountLong, @"exit", @"entryMarket");
							}
						}
						else
						{
							switch(EMAForTrailing)
							  {                        
							    case 1:
									if ((Close[0] < iEMA3[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
									{
										Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
										ExitLong(FixAmountLong, @"exit", @"entryMarket");
									}
							        break;        
							    case 2:
									if ((Close[0] < iEMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
									{
										Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
										ExitLong(FixAmountLong, @"exit", @"entryMarket");
									}
							        break;							       																
							  }
						}
					}
					else
					{
						switch(EMAForTrailing)
						  {                        
						    case 1:
								if ((Close[0] < iEMA3[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryMarket");
								}
						        break;        
						    case 2:
								if ((Close[0] < iEMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iEMA1[0] - iATR[0] * (MagicNumber / 100)) && isTrailing)
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryMarket");
								}
						        break;							       																
						  }
					}
					if (High[0] >= FixTriggerPriceLong)
					{
						FixStopPriceLong = High[0] - FixStopSize * TrailingUnitsStop;
						FixTriggerPriceLong = High[0];
						ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryMarket");
						isTrailing = true;
						Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
					}
					if (isTrailing && iEMA2[0] - iATR[0] > FixStopPriceLong)
					{
						FixStopPriceLong = iEMA2[0] - iATR[0] * EMAShield;
						ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
					}
				}
			}
			
			///While Short	
			if (Position.MarketPosition == MarketPosition.Short && !isShort) //if the postition is stil active and the initial stop and trailing stop trigger price levels were set then...
			{
				isLong = false;
				isShort = true;
				FixAmountShort = AmountShort;
				FixStopPriceShort = StopPriceShort;
				FixTriggerPriceShort = TriggerPriceShort;
				FixDistanceToPullbackClose = DistanceToPullbackClose;
				Draw.FibonacciRetracements(this, "tag1" + CurrentBar, false, 0, Position.AveragePrice, 10, FixStopPriceShort);
			}				
			if (Position.MarketPosition == MarketPosition.Short && isShort)
			{
				if (myEntryOrder == null || myEntryMarket == null)
				{
					if (myEntryMarket == null && myEntryOrder.OrderState == OrderState.Filled)
					{
						ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryOrder");
//						}
//					}
						if (Low[BarsSinceEntryExecution(0, @"entryOrder", 0)] > iEMA2[BarsSinceEntryExecution(0, @"entryOrder", 0)] - TicksToBO * TickSize)
						{
							bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i=0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
							{
								if (Low[i] <= iEMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
					        		LowCrossEma50 = true; //And saves that value in this variable
									break;
								}								
							}								
							if (LowCrossEma50 == false)
							{
								if ((Close[0] > iEMA1[0] + iATR[0] * (MagicNumber / 100)) && isTrailing)
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
									ExitShort(FixAmountShort, @"exit", @"entryOrder");
								}
							}
							else
							{
								if ((Close[0] > iEMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iEMA1[0] + iATR[0] * (MagicNumber / 100)) && isTrailing)
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
									ExitShort(FixAmountShort, @"exit", @"entryOrder");
								}
							}
						}
						else
						{
							if ((Close[0] > iEMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iEMA1[0] + iATR[0] * (MagicNumber / 100)) && isTrailing)
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(FixAmountShort, @"exit", @"entryOrder");
							}
						}
						if (Low[0] <= FixTriggerPriceShort)
						{
							FixStopPriceShort = Low[0] + FixDistanceToPullbackClose * TrailingUnitsStop;
							FixTriggerPriceShort = Low[0];
							ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryOrder");
							isTrailing = true;
							Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Red);
						}
						if (isTrailing && iEMA2[0] + iATR[0] < FixStopPriceLong)
						{
							FixStopPriceLong = iEMA2[0] + iATR[0];
							ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
						}
					}
					else if (myEntryOrder == null && myEntryMarket.OrderState == OrderState.Filled)
					{
						ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryMarket");
//						}
//					}
						if (Low[BarsSinceEntryExecution(0, @"entryMarket", 0)] > iEMA2[BarsSinceEntryExecution(0, @"entryMarket", 0)] - TicksToBO * TickSize)
						{
							bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i=0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
							{
								if (Low[i] <= iEMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
					        		LowCrossEma50 = true; //And saves that value in this variable
									break;
								}								
							}								
							if (LowCrossEma50 == false)
							{
								if ((Close[0] > iEMA1[0] + iATR[0] * (MagicNumber / 100)) && isTrailing)
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
									ExitShort(FixAmountShort, @"exit", @"entryMarket");
								}
							}
							else
							{
								if ((Close[0] > iEMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iEMA1[0] + iATR[0] * (MagicNumber / 100)) && isTrailing)
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
									ExitShort(FixAmountShort, @"exit", @"entryMarket");
								}
							}
						}
						else
						{
							if ((Close[0] > iEMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iEMA1[0] + iATR[0] * (MagicNumber / 100)) && isTrailing)
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(FixAmountShort, @"exit", @"entryMarket");
							}
						}
						if (Low[0] <= FixTriggerPriceShort)
						{
							FixStopPriceShort = Low[0] + FixDistanceToPullbackClose * TrailingUnitsStop;
							FixTriggerPriceShort = Low[0];
							ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryMarket");
							isTrailing = true;
							Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Red);
						}
						if (isTrailing && iEMA2[0] + iATR[0] < FixStopPriceLong)
						{
							FixStopPriceLong = iEMA2[0] + iATR[0];
							ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
						}
					}
				}
				else if (myEntryOrder.OrderState == OrderState.Filled/* && myEntryMarket.OrderState != OrderState.Filled*/)
				{
//					if (myEntryOrder.OrderState == OrderState.Filled && myEntryMarket.OrderState != OrderState.Filled)
//					{
//						if (myEntryOrder.OrderType == OrderType.StopMarket && myEntryOrder.OrderState == OrderState.Working && myEntryOrder.OrderAction == OrderAction.Buy)
//						{
//							Print (String.Format("{0} // {1} // {2} // {3}", myEntryOrder.StopPrice, FixStopPriceLong,  myEntryOrder.Quantity, Time[0]));
//						}
//						else
//						{
							ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryOrder");
//						}
//					}
					if (Low[BarsSinceEntryExecution(0, @"entryOrder", 0)] > iEMA2[BarsSinceEntryExecution(0, @"entryOrder", 0)] - TicksToBO * TickSize)
					{
						bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i=0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
						{
							if (Low[i] <= iEMA2[i]) //To determine the highest value (max close value of the swinglow4)
							{
				        		LowCrossEma50 = true; //And saves that value in this variable
								break;
							}								
						}								
						if (LowCrossEma50 == false)
						{
							if ((Close[0] > iEMA1[0] + iATR[0] * (MagicNumber / 100)) && isTrailing)
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(FixAmountShort, @"exit", @"entryOrder");
							}
						}
						else
						{
							if ((Close[0] > iEMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iEMA1[0] + iATR[0] * (MagicNumber / 100)) && isTrailing)
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(FixAmountShort, @"exit", @"entryOrder");
							}
						}
					}
					else
					{
						if ((Close[0] > iEMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iEMA1[0] + iATR[0] * (MagicNumber / 100)) && isTrailing)
						{
							Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
							ExitShort(FixAmountShort, @"exit", @"entryOrder");
						}
					}
					if (Low[0] <= FixTriggerPriceShort)
					{
						FixStopPriceShort = Low[0] + FixDistanceToPullbackClose * TrailingUnitsStop;
						FixTriggerPriceShort = Low[0];
						ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryOrder");
						isTrailing = true;
						Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Red);
					}
					if (isTrailing && iEMA2[0] + iATR[0] < FixStopPriceLong)
					{
						FixStopPriceLong = iEMA2[0] + iATR[0];
						ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
					}
				}
				else if (myEntryMarket.OrderState == OrderState.Filled/* && myEntryOrder.OrderState != OrderState.Filled*/)
				{
//					if (myEntryMarket.OrderState == OrderState.Filled && myEntryOrder.OrderState != OrderState.Filled)
//					{
						if (myEntryOrder.OrderType == OrderType.StopMarket && myEntryOrder.OrderState == OrderState.Working && myEntryOrder.OrderAction == OrderAction.Buy)
						{
//							Print (String.Format("{0} // {1} // {2} // {3}", myEntryOrder.StopPrice, FixStopPriceShort,  myEntryMarket.Quantity, Time[0]));
						}
						else
						{
							ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryMarket");
						}
//					}
					if (Low[BarsSinceEntryExecution(0, @"entryMarket", 0)] > iEMA2[BarsSinceEntryExecution(0, @"entryMarket", 0)] - TicksToBO * TickSize)
					{
						bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i=0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
						{
							if (Low[i] <= iEMA2[i]) //To determine the highest value (max close value of the swinglow4)
							{
				        		LowCrossEma50 = true; //And saves that value in this variable
								break;
							}								
						}								
						if (LowCrossEma50 == false)
						{
							if ((Close[0] > iEMA1[0] + iATR[0] * (MagicNumber / 100)) && isTrailing)
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(FixAmountShort, @"exit", @"entryMarket");
							}
						}
						else
						{
							if ((Close[0] > iEMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iEMA1[0] + iATR[0] * (MagicNumber / 100)) && isTrailing)
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(FixAmountShort, @"exit", @"entryMarket");
							}
						}
					}
					else
					{
						if ((Close[0] > iEMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iEMA1[0] + iATR[0] * (MagicNumber / 100)) && isTrailing)
						{
							Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
							ExitShort(FixAmountShort, @"exit", @"entryMarket");
						}
					}
					if (Low[0] <= FixTriggerPriceShort)
					{
						FixStopPriceShort = Low[0] + FixDistanceToPullbackClose * TrailingUnitsStop;
						FixTriggerPriceShort = Low[0];
						ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryMarket");
						isTrailing = true;
						Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Red);
					}
					if (isTrailing && iEMA2[0] + iATR[0] < FixStopPriceLong)
					{
						FixStopPriceLong = iEMA2[0] + iATR[0];
						ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
					}
				}
			}
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA1", Order=1, GroupName="Parameters")]
		public int EMA1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA2", Order=2, GroupName="Parameters")]
		public int EMA2
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA3", Order=3, GroupName="Parameters")]
		public int EMA3
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="UnitsTriggerForTrailing", Order=4, GroupName="Parameters")]
		public double UnitsTriggerForTrailing
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="TrailingUnitsStop", Order=5, GroupName="Parameters")]
		public double TrailingUnitsStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ATR1", Order=6, GroupName="Parameters")]
		public int ATR1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Swing1", Order=7, GroupName="Parameters")]
		public int Swing1
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Swing2", Order=8, GroupName="Parameters")]
		public int Swing2
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="RiskUnit", Order=9, GroupName="Parameters")]
		public int RiskUnit
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="IncipientTrendFactor", Order=10, GroupName="Parameters")]
		public double IncipientTrendFactor
		{ get; set; }
				
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="ATRSMAFactor", Order=11, GroupName="Parameters")]
		public double ATRSMAFactor
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="TicksToBO", Order=12, GroupName="Parameters")]
		public double TicksToBO
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1000)]
		[Display(Name="SwingPenetration(%)", Order=13, GroupName="Parameters")]
		public double SwingPenetration
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="ClosnessFactor", Order=14, GroupName="Parameters")]
		public double ClosnessFactor
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="ClosnessToTrade", Order=15, GroupName="Parameters")]
		public double ClosnessToTrade
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="MagicNumber(Percent (%) of ATR)", Order=16, GroupName="Parameters")]
		public double MagicNumber
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="ATRPullbackFactor", Order=17, GroupName="Parameters")]
		public double ATRPullbackFactor
		{ get; set; }
				
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="ATRStopFactor", Order=18, GroupName="Parameters")]
		public double ATRStopFactor
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="ATR_EMA_cross_factor", Order=19, GroupName="Parameters")]
		public double ATR_EMA_cross_factor
		{ get; set; }
				
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="EMAForTrailing", Order=20, GroupName="Parameters")]
		public int EMAForTrailing
		{ get; set; }
				
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="EMAShield", Order=21, GroupName="Parameters")]
		public double EMAShield
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(-100, 100)]
		[Display(Name="VolumenPercent(Percent (%) of VOLMA)", Order=22, GroupName="Parameters")]
		public double VolumenPercent
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name="HLorLH", Order=23, GroupName="Parameters")]
		public int HLorLH
		{ get; set; }
		#endregion
		
		#region Functions
////	Swing Identification Method				
		public bool SwingIdentifiacation(ISeries<double> BarClose, ISeries<double> OppositeSwing, int ReferenceSwingBar, bool isPotentialSwing, string SwingType) 
		{		
			if (SwingType == "SwingHigh")
			{
				for (int i=0; i <= ReferenceSwingBar; i++) // Walks every bar of the potential HL4
				{																
					if (BarClose[i] < OppositeSwing[i + 1] - TicksToBO * TickSize) //if there is any violation (close below a former swinglow) within the swing then...
					{
						isPotentialSwing = false; //invalidates the swing high 4 (HL4)
						break;
					}
					if (BarClose[i] < OppositeSwing[ReferenceSwingBar + 1] - TicksToBO * TickSize) // if there is any close below the Last OppositeSwing before the current swinghigh4 reference for the entrance (the one that originated the inicial movement) then...
					{
						isPotentialSwing = false; // invalidates the swing high 4 (HL4)
						break;
					}				
				}		
			}
			else if (SwingType == "SwingLow")
			{
				for (int i=0; i <= ReferenceSwingBar; i++) // Walks every bar of the potential HL4
				{																
					if (BarClose[i] > OppositeSwing[i + 1] + TicksToBO * TickSize) //if there is any violation (close below a former swinglow) within the swing then...
					{
						isPotentialSwing = false; //invalidates the swing high 4 (HL4)
						break;
					}
					if (BarClose[i] > OppositeSwing[ReferenceSwingBar + 1] + TicksToBO * TickSize) // if there is any close below the Last OppositeSwing before the current swinghigh4 reference for the entrance (the one that originated the inicial movement) then...
					{
						isPotentialSwing = false; // invalidates the swing high 4 (HL4)
						break;
					}				
				}		
			}
			return isPotentialSwing;		
		}
	
////	Swing Location and Characterization Method				
		public Tuple<double, double, double, int> SwingLocation(ISeries<double> BarExtreme, ISeries<double> ReferenceSwing, int ReferenceSwingBar, string SwingType) 
		{	
			double SwingMidLevel = ReferenceSwing[0];
			double ExtremeLevel = BarExtreme[0]; //initializing the variable that is going to keep the max high value of the swing, with the array firts value for comparison purposes
//			double ExtremeClose = Close[0];
			int ExtremeLevelBar = 0;
			if (SwingType == "SwingHigh")
			{
				double MinClose = Close[0]; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
				for (int i=0; i <= ReferenceSwingBar; i++) // Walks every bar of the potential HL4
				{
					if (Close[i] < MinClose) //To determine the highest value (max close value of the swinglow4)
					{
		        		MinClose = Close[i]; //And saves that value in this variable
					}
					if (BarExtreme[i] > ExtremeLevel) //To determine the highest value (max close value of the swinglow4)
					{
						ExtremeLevel = BarExtreme[i]; //And saves that value in this variable
						ExtremeLevelBar = i;
					}
				}	
				SwingMidLevel = ReferenceSwing[0] - ((ReferenceSwing[0] - MinClose) * (SwingPenetration / 100));
				ExtremeClose = MinClose;
			}
			else if (SwingType == "SwingLow")
			{
				double MaxClose = Close[0]; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
				for (int i=0; i <= ReferenceSwingBar; i++) // Walks every bar of the potential HL4
				{
					if (Close[i] > MaxClose) //To determine the highest value (max close value of the swinglow4)
					{
		        		MaxClose = Close[i]; //And saves that value in this variable
					}
					if (BarExtreme[i] < ExtremeLevel) //To determine the highest value (max close value of the swinglow4)
					{
						ExtremeLevel = BarExtreme[i]; //And saves that value in this variable
						ExtremeLevelBar = i;
					}
				}	
				SwingMidLevel = ReferenceSwing[0] + ((MaxClose - ReferenceSwing[0]) * (SwingPenetration / 100));
				ExtremeClose = MaxClose;
			}
			return new Tuple<double, double, double, int>(ExtremeLevel, SwingMidLevel, ExtremeClose, ExtremeLevelBar);	
		}
		
////	Buy Process	Method		
		public bool Buy(string OrderType, ISeries<double> BOLevel) 
		{
			if (iVOL[1] > iVOLMA[1] * (1 + (VolumenPercent / 100)))
			{
				if (OrderType == "MarketOrder")
				{
//					AmountLong = Convert.ToInt32((RiskUnit / ((StopSize / TickSize) * Instrument.MasterInstrument.PointValue * TickSize))); //calculates trade amount
					AmountLong = Convert.ToInt32(RiskUnit / StopSize); //calculates trade amount
					EnterLong(AmountLong, @"entryMarket"); // Long Stop order activation							
//					StopPriceLong = Position.AveragePrice - ATRStopFactor; //calculates the stop price level
//					TriggerPriceLong = Position.AveragePrice + ATRStopFactor * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger	
					return true;
				}
				else if (OrderType == "PendingOrder")
				{
					AmountLong = Convert.ToInt32((RiskUnit / ((StopSize / TickSize) * Instrument.MasterInstrument.PointValue * TickSize)));
					EnterLongStopMarket(AmountLong, BOLevel[0] + TicksToBO * TickSize, @"entryOrder");							
					StopPriceLong = (BOLevel[0] + TicksToBO * TickSize) - ATRStopFactor; //calculates the stop price level
					TriggerPriceLong = (BOLevel[0] + TicksToBO * TickSize) + ATRStopFactor * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger							
					return false;
				}	
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}
		
////	Sell Process Method	 			
//		public bool Sell(string OrderType, ISeries<double> BOLevel) 
//		{
//			if (iEMA2[0] < iEMA1[0]  && iEMA1[0] - iEMA2[0] < iATR[0] * 2)
//			{
//				if (OrderType == "MarketOrder")
//				{
//					AmountShort = Convert.ToInt32((RiskUnit / ((DistanceToPullbackClose / TickSize) * Instrument.MasterInstrument.PointValue * TickSize))); //calculates trade amount
//					EnterShort(AmountShort, @"entryMarket"); // Long Stop order activation							
//					StopPriceShort = Close[0] + DistanceToPullbackClose; //calculates the stop price level
//					TriggerPriceShort = Close[0] - DistanceToPullbackClose * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger	
//					return true;
//				}
//				else if (OrderType == "PendingOrder")
//				{
//					AmountShort = Convert.ToInt32((RiskUnit / ((DistanceToPullbackClose / TickSize) * Instrument.MasterInstrument.PointValue * TickSize)));
//					EnterShortStopMarket(AmountShort, BOLevel[0] - TicksToBO * TickSize, @"entryOrder");							
//					StopPriceShort = (BOLevel[0] - TicksToBO * TickSize) + DistanceToPullbackClose; //calculates the stop price level
//					TriggerPriceShort = (BOLevel[0] - TicksToBO * TickSize) - DistanceToPullbackClose * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger					
//					Print (String.Format("{0} // {1} // {2} // {3}", AmountShort, BOLevel[0], StopPriceShort, Time[0]));
//					return false;
//				}
//				else
//				{
//					return false;
//				}
//			}
//			else
//			{
//				return false;
//			}
//		}
		
////	Swing4 operation Method			
		///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4	
		public Tuple<bool, bool, bool> Swing4(ISeries<double> ReferenceSMA, string Type) 
        {	
////		SWING 4 HIGH			
			if (Type == "High")
			{
				bool isSwingHigh4 = true;
				if (HLorLH == 1)
				{
					isSwingHigh4 = SwingIdentifiacation(Close, iSwing1.SwingLow, iSwing1.SwingHighBar(0, 1, CurrentBar), true, "SwingHigh");
				}
				bool isActiveLongPosition = false;				
				if (isSwingHigh4) //if the HL4 is confirmated after the Last two validations then...
				{		
					Tuple<double, double, double, int> ReturnedValues = SwingLocation(High, iSwing1.SwingHigh, iSwing1.SwingHighBar(0, 1, CurrentBar), "SwingHigh");
					max_high_swingHigh1 = ReturnedValues.Item1;
					double SwingHigh4MidLevel = ReturnedValues.Item2;
//					double ExtremeClose = ReturnedValues.Item3;
					int ExtremeLevelBar = ReturnedValues.Item4;
					if (iSwing1.SwingHigh[0] - ExtremeClose <= DistanceToPullbackClose)
					{
						if (SwingHigh4MidLevel >= ReferenceSMA[0] /*&& max_high_swingHigh1 < iSwing1.SwingHigh[0] + TicksToBO * TickSize*/)
						{
							if (iSwing2.SwingHigh[0] >= iSwing1.SwingHigh[0] && iSwing2.SwingHigh[0] <= iSwing1.SwingHigh[0] + iATR[0] * ClosnessFactor)
							{
								if (((iSwing2.SwingHigh[0] + TicksToBO * TickSize) - iEMA1[0] <= DistanceToSMA || (iSwing2.SwingHigh[0] + TicksToBO * TickSize) - iEMA2[0] <= DistanceToSMA)/* && (iSwing2.SwingHigh[0] + TicksToBO * TickSize) - ExtremeClose <= DistanceToPullbackClose*/)
								{
									if (myEntryOrder != null && myExitOrder != null)
									{
										if ((myEntryOrder.OrderType == OrderType.StopMarket || myExitOrder.OrderType == OrderType.StopMarket) && (myEntryOrder.OrderState == OrderState.Working || myEntryOrder.OrderState == OrderState.Filled || myExitOrder.OrderState == OrderState.Working) && (myEntryOrder.OrderAction == OrderAction.SellShort || myExitOrder.OrderAction == OrderAction.SellShort))
										{	
											Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing2.SwingHigh, iSwing1.SwingHigh, ReferenceSMA, ExtremeLevelBar, is_BO_up_swing1, max_high_swingHigh1, "Up");
											is_BO_up_swing1 = ReturnedValues1.Item1;
											isActiveLongPosition = ReturnedValues1.Item2;
										}
										else
										{									
											if (max_high_swingHigh1 < iSwing2.SwingHigh[0] + TicksToBO * TickSize)
											{
												Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing2.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Lime); //Draw a mark to know where all conditions have been met
												isActiveLongPosition = Buy("PendingOrder", iSwing2.SwingHigh);
												 
											}
											else 
											{
												is_BO_up_swing1 = true;
											}
										}
									}
									else
									{
										if (max_high_swingHigh1 < iSwing2.SwingHigh[0] + TicksToBO * TickSize)
										{
											Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing2.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Lime); //Draw a mark to know where all conditions have been met
											isActiveLongPosition = Buy("PendingOrder", iSwing2.SwingHigh);
											 
										}
										else 
										{
											is_BO_up_swing1 = true;
										}
									}
								}
							}
							else 
							{
								if (myEntryOrder != null && myExitOrder != null)
								{
									if ((myEntryOrder.OrderType == OrderType.StopMarket || myExitOrder.OrderType == OrderType.StopMarket) && (myEntryOrder.OrderState == OrderState.Working || myEntryOrder.OrderState == OrderState.Filled || myExitOrder.OrderState == OrderState.Working) && (myEntryOrder.OrderAction == OrderAction.SellShort || myExitOrder.OrderAction == OrderAction.SellShort))
									{	
										Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing1.SwingHigh, iSwing1.SwingHigh, ReferenceSMA, ExtremeLevelBar, is_BO_up_swing1, max_high_swingHigh1, "Up");
										is_BO_up_swing1 = ReturnedValues1.Item1;
										isActiveLongPosition = ReturnedValues1.Item2;
									}
									else
									{								
										if (max_high_swingHigh1 < iSwing1.SwingHigh[0] + TicksToBO * TickSize)
										{
											Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing1.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Lime); //Draw a mark to know where all conditions have been met
											isActiveLongPosition = Buy("PendingOrder", iSwing1.SwingHigh);
											 
										}
										else
										{
											is_BO_up_swing1 = true;
										}
									}
								}
								else
								{
									if (max_high_swingHigh1 < iSwing1.SwingHigh[0] + TicksToBO * TickSize)
										{
											Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing1.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Lime); //Draw a mark to know where all conditions have been met
											isActiveLongPosition = Buy("PendingOrder", iSwing1.SwingHigh);
											 
										}
										else
										{
											is_BO_up_swing1 = true;
										}
								}
							}
						}
						else if (iSwing1.SwingHigh[0] >= ReferenceSMA[0])	
						{
							if (iSwing2.SwingHigh[0] >= iSwing1.SwingHigh[0] && iSwing2.SwingHigh[0] <= iSwing1.SwingHigh[0] + iATR[0] * ClosnessFactor)
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing2.SwingHigh, ReferenceSMA, max_high_swingHigh1, ExtremeLevelBar, 0, "Up", "Swing4", is_BO_up_swing1);
								is_BO_up_swing1 = ReturnedValues1.Item1;
								isActiveLongPosition = ReturnedValues1.Item2;
							}
							else
							{	
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing4(iSwing1.SwingHigh, ReferenceSMA, ExtremeLevelBar, 0, "Up");
								is_BO_up_swing1 = ReturnedValues1.Item1;
								isActiveLongPosition = ReturnedValues1.Item2;
							}
						}
						else 
						{	
							if (iSwing2.SwingHigh[0] >= iSwing1.SwingHigh[0] && iSwing2.SwingHigh[0] <= iSwing1.SwingHigh[0] + iATR[0] * ClosnessFactor)
							{
								if (iSwing2.SwingHigh[0] < ReferenceSMA[0])
								{	
									Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(ReferenceSMA, ReferenceSMA, max_high_swingHigh1, ExtremeLevelBar, ExtremeLevelBar, "Up", "Swing4", is_BO_up_swing1);
									is_BO_up_swing1 = ReturnedValues1.Item1;
									isActiveLongPosition = ReturnedValues1.Item2;
								}	
								else
								{
									Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing2.SwingHigh, ReferenceSMA, max_high_swingHigh1, ExtremeLevelBar, 0, "Up", "Swing4", is_BO_up_swing1);
									is_BO_up_swing1 = ReturnedValues1.Item1;
									isActiveLongPosition = ReturnedValues1.Item2;
								}
							}
							else
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing4(ReferenceSMA, ReferenceSMA, ExtremeLevelBar, ExtremeLevelBar, "Up");
								is_BO_up_swing1 = ReturnedValues1.Item1;
								isActiveLongPosition = ReturnedValues1.Item2;
							}
						}
					}
				}
				if (is_BO_up_swing1)
				{
					isSwingHigh4 = false;
				}
				return new Tuple<bool, bool, bool>(isSwingHigh4, isActiveLongPosition, is_BO_up_swing1);
			}
			
////		SWING 4 LOW
			else if (Type == "Low")
			{
				bool isSwingLow4 = true;
				if (HLorLH == 1)
				{
					isSwingLow4 = SwingIdentifiacation(Close, iSwing1.SwingHigh, iSwing1.SwingLowBar(0, 1, CurrentBar), true, "SwingLow");
				}
				bool isActiveShortPosition = false;
				if (isSwingLow4)
				{
					Tuple<double, double, double, int> ReturnedValues = SwingLocation(Low, iSwing1.SwingLow, iSwing1.SwingLowBar(0, 1, CurrentBar), "SwingLow");
					min_low_swingLow1 = ReturnedValues.Item1;
					double SwingLow4MidLevel = ReturnedValues.Item2;
//					double ExtremeClose = ReturnedValues.Item3;
					int ExtremeLevelBar = ReturnedValues.Item4;
					if (ExtremeClose - iSwing1.SwingLow[0] <= DistanceToPullbackClose)
					{				
						if (SwingLow4MidLevel <= ReferenceSMA[0] /*&& min_low_swingLow1 > iSwing1.SwingLow[0] - TicksToBO * TickSize*/)
						{
							if (iSwing2.SwingLow[0] <= iSwing1.SwingLow[0] && iSwing2.SwingLow[0] >= iSwing1.SwingLow[0] - iATR[0] * ClosnessFactor)
							{
								if ((iEMA1[0] - iSwing2.SwingLow[0] - TicksToBO * TickSize <= DistanceToSMA || iEMA2[0] - iSwing2.SwingLow[0] - TicksToBO * TickSize <= DistanceToSMA)/* && ExtremeClose - iSwing2.SwingLow[0] - TicksToBO * TickSize <= DistanceToPullbackClose*/)
								{
									if (myEntryOrder != null && myExitOrder != null)
									{
										if ((myEntryOrder.OrderType == OrderType.StopMarket || myExitOrder.OrderType == OrderType.StopMarket) && (myEntryOrder.OrderState == OrderState.Working || myEntryOrder.OrderState == OrderState.Filled || myExitOrder.OrderState == OrderState.Working) && (myEntryOrder.OrderAction == OrderAction.Buy || myExitOrder.OrderAction == OrderAction.Buy))
										{
											Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing2.SwingLow, iSwing1.SwingLow, ReferenceSMA, ExtremeLevelBar, is_BO_down_swing1, min_low_swingLow1, "Down");
											is_BO_down_swing1 = ReturnedValues1.Item1;
											isActiveShortPosition = ReturnedValues1.Item2;
										}
										else
										{									
											if (min_low_swingLow1 > iSwing2.SwingLow[0] - TicksToBO * TickSize)
											{
												Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing2.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Red);
//												isActiveShortPosition = Sell("PendingOrder", iSwing2.SwingLow);
											}
											else
											{
												is_BO_down_swing1 = true;
											}
										}
									}
									else
									{
										if (min_low_swingLow1 > iSwing2.SwingLow[0] - TicksToBO * TickSize)
										{
											Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing2.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Red);
//											isActiveShortPosition = Sell("PendingOrder", iSwing2.SwingLow);
										}
										else
										{
											is_BO_down_swing1 = true;
										}
									}
								}
							}
							else 
							{	
								if (myEntryOrder != null && myExitOrder != null)
								{
									if ((myEntryOrder.OrderType == OrderType.StopMarket || myExitOrder.OrderType == OrderType.StopMarket) && (myEntryOrder.OrderState == OrderState.Working || myEntryOrder.OrderState == OrderState.Filled || myExitOrder.OrderState == OrderState.Working) && (myEntryOrder.OrderAction == OrderAction.Buy || myExitOrder.OrderAction == OrderAction.Buy))
									{	
										Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing1.SwingLow, iSwing1.SwingLow, ReferenceSMA, ExtremeLevelBar, is_BO_down_swing1, min_low_swingLow1, "Down");
										is_BO_down_swing1 = ReturnedValues1.Item1;
										isActiveShortPosition = ReturnedValues1.Item2;
									}
									else
									{									
										if (min_low_swingLow1 > iSwing1.SwingLow[0] - TicksToBO * TickSize)
										{
											Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing1.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Red);
//											isActiveShortPosition = Sell("PendingOrder", iSwing1.SwingLow);
										}
										else
										{
											is_BO_down_swing1 = true;
										}
									}
								}
								else
								{
									if (min_low_swingLow1 > iSwing1.SwingLow[0] - TicksToBO * TickSize)
									{
										Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing1.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Red);
//										isActiveShortPosition = Sell("PendingOrder", iSwing1.SwingLow);
									}
									else
									{
										is_BO_down_swing1 = true;
									}
								}
							}
						} 
						else if (iSwing1.SwingLow[0] <= ReferenceSMA[0])
						{
							if (iSwing2.SwingLow[0] <= iSwing1.SwingLow[0] && iSwing2.SwingLow[0] >= iSwing1.SwingLow[0] - iATR[0] * ClosnessFactor)
							{		
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing2.SwingLow, ReferenceSMA, min_low_swingLow1, ExtremeLevelBar, 0, "Down", "Swing4", is_BO_down_swing1);
								is_BO_down_swing1 = ReturnedValues1.Item1;
								isActiveShortPosition = ReturnedValues1.Item2;
							}
							else
							{
	//							Print (String.Format("{0} // {1} // {2} // {3}", is_BO_down_swing1, iSwing2.SwingLow[0], iSwing1.SwingLow[0], Time[0]));
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing4(iSwing1.SwingLow, ReferenceSMA, ExtremeLevelBar, 0, "Down");
								is_BO_down_swing1 = ReturnedValues1.Item1;
								isActiveShortPosition = ReturnedValues1.Item2;
							}
						}	
						else
						{
							if (iSwing2.SwingLow[0] <= iSwing1.SwingLow[0] && iSwing2.SwingLow[0] >= iSwing1.SwingLow[0] - iATR[0] * ClosnessFactor)
							{																		
								if (iSwing2.SwingLow[0] > ReferenceSMA[0])
								{	
									Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(ReferenceSMA, ReferenceSMA, min_low_swingLow1, ExtremeLevelBar, ExtremeLevelBar, "Down", "Swing4", is_BO_down_swing1);
									is_BO_down_swing1 = ReturnedValues1.Item1;
									isActiveShortPosition = ReturnedValues1.Item2;
								}
								else
								{
									Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing2.SwingLow, ReferenceSMA, min_low_swingLow1, ExtremeLevelBar, 0, "Down", "Swing4", is_BO_down_swing1);
									is_BO_down_swing1 = ReturnedValues1.Item1;
									isActiveShortPosition = ReturnedValues1.Item2;
								}
							}
							else
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing4(ReferenceSMA, ReferenceSMA, ExtremeLevelBar, ExtremeLevelBar, "Down");
								is_BO_down_swing1 = ReturnedValues1.Item1;
								isActiveShortPosition = ReturnedValues1.Item2;							
							}
						}	
					}
				}
				if (is_BO_down_swing1)
				{
					isSwingLow4 = false;
				}
				return new Tuple<bool, bool, bool>(isSwingLow4, isActiveShortPosition, is_BO_down_swing1);
			}
			return new Tuple<bool, bool, bool>(false, false, false);
		}
		
////	Swing14 operation Method				
		public Tuple<bool, bool, bool> Swing14(ISeries<double> ReferenceSMA, string Type) 
        {	
////		SWING 14 HIGH
			if (Type == "High")
			{
				bool isSwingHigh14 = true;
				if (HLorLH == 1)
				{
					isSwingHigh14 = SwingIdentifiacation(Close, iSwing2.SwingLow, iSwing2.SwingHighBar(0, 1, CurrentBar), true, "SwingHigh");
				}
				bool isActiveLongPosition = false;
				if (isSwingHigh14)
				{	
					Tuple<double, double, double, int> ReturnedValues = SwingLocation(High, iSwing2.SwingHigh, iSwing2.SwingHighBar(0, 1, CurrentBar), "SwingHigh");
					max_high_swingHigh2 = ReturnedValues.Item1;
					double SwingHigh14MidLevel = ReturnedValues.Item2;
//					double ExtremeClose = ReturnedValues.Item3;
					int ExtremeLevelBar = ReturnedValues.Item4;
					if (iSwing2.SwingHigh[0] - ExtremeClose <= DistanceToPullbackClose)
					{
						if (SwingHigh14MidLevel >= ReferenceSMA[0]  /*&& max_high_swingHigh2 < iSwing2.SwingHigh[0] + TicksToBO * TickSize*/)
						{	
							if (myEntryOrder != null && myExitOrder != null)
							{
								if ((myEntryOrder.OrderType == OrderType.StopMarket || myExitOrder.OrderType == OrderType.StopMarket) && (myEntryOrder.OrderState == OrderState.Working || myEntryOrder.OrderState == OrderState.Filled || myExitOrder.OrderState == OrderState.Working) && (myEntryOrder.OrderAction == OrderAction.SellShort || myExitOrder.OrderAction == OrderAction.SellShort))
								{
									Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing2.SwingHigh, iSwing2.SwingHigh, ReferenceSMA, ExtremeLevelBar, is_BO_up_swing2, max_high_swingHigh2, "Up");
									is_BO_up_swing2 = ReturnedValues1.Item1;
									isActiveLongPosition = ReturnedValues1.Item2;
								}
								else
								{
									
									if (max_high_swingHigh2 < iSwing2.SwingHigh[0] + TicksToBO * TickSize)
									{						
										Draw.Dot(this, @"GreenDot" + CurrentBar, true, 0, iSwing2.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Green);		
										isActiveLongPosition = Buy("PendingOrder", iSwing2.SwingHigh);
									}
									else
									{
										is_BO_up_swing2 = true;
									}
								}
							}
							else
							{
								if (max_high_swingHigh2 < iSwing2.SwingHigh[0] + TicksToBO * TickSize)
								{						
									Draw.Dot(this, @"GreenDot" + CurrentBar, true, 0, iSwing2.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Green);		
									isActiveLongPosition = Buy("PendingOrder", iSwing2.SwingHigh);
								}
								else
								{
									is_BO_up_swing2 = true;
								}
							}
						}
						else if (iSwing2.SwingHigh[0] >= ReferenceSMA[0])
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing2.SwingHigh, ReferenceSMA, max_high_swingHigh2, ExtremeLevelBar, 0, "Up", "Swing14", is_BO_up_swing2);
							is_BO_up_swing2 = ReturnedValues1.Item1;
							isActiveLongPosition = ReturnedValues1.Item2;
						}
						else
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(ReferenceSMA, ReferenceSMA, max_high_swingHigh2, ExtremeLevelBar, ExtremeLevelBar, "Up", "Swing14", is_BO_up_swing2);
							is_BO_up_swing2 = ReturnedValues1.Item1;
							isActiveLongPosition = ReturnedValues1.Item2;
						}
					}
				}
				if (is_BO_up_swing2)
				{
					isSwingHigh14 = false;
				}
				return new Tuple<bool, bool, bool>(isSwingHigh14, isActiveLongPosition, is_BO_up_swing2);
			}
			
////		SWING 14 LOW
			else if (Type == "Low")
			{
				bool isSwingLow14 = true;
				if (HLorLH == 1)
				{
					isSwingLow14 = SwingIdentifiacation(Close, iSwing2.SwingHigh, iSwing2.SwingLowBar(0, 1, CurrentBar), true, "SwingLow");
				}
				bool isActiveShortPosition = false;
				if(isSwingLow14)
				{
					Tuple<double, double, double, int> ReturnedValues = SwingLocation(Low, iSwing2.SwingLow, iSwing2.SwingLowBar(0, 1, CurrentBar), "SwingLow");
					min_low_swingLow2 = ReturnedValues.Item1;	
					double SwingLow14MidLevel = ReturnedValues.Item2;
//					double ExtremeClose = ReturnedValues.Item3;
					int ExtremeLevelBar = ReturnedValues.Item4;
					if (ExtremeClose - iSwing2.SwingLow[0] <= DistanceToPullbackClose)
					{
						if (SwingLow14MidLevel <= ReferenceSMA[0] /*&& min_low_swingLow2 > iSwing2.SwingLow[0] - TicksToBO * TickSize*/)
						{	
							if (myEntryOrder != null && myExitOrder != null)
							{
								if ((myEntryOrder.OrderType == OrderType.StopMarket || myExitOrder.OrderType == OrderType.StopMarket) && (myEntryOrder.OrderState == OrderState.Working || myEntryOrder.OrderState == OrderState.Filled || myExitOrder.OrderState == OrderState.Working) && (myEntryOrder.OrderAction == OrderAction.Buy || myExitOrder.OrderAction == OrderAction.Buy))
								{
									Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing2.SwingLow, iSwing2.SwingLow, ReferenceSMA, ExtremeLevelBar, is_BO_down_swing2, min_low_swingLow2, "Down");
									is_BO_down_swing2 = ReturnedValues1.Item1;
									isActiveShortPosition = ReturnedValues1.Item2;
								}
								else
								{						
									if (min_low_swingLow2 > iSwing2.SwingLow[0] - TicksToBO * TickSize)
									{					
										Draw.Dot(this, @"MaroonDot" + CurrentBar, true, 0, iSwing2.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Maroon);	
//										isActiveShortPosition = Sell("PendingOrder", iSwing2.SwingLow);
									}
									else
									{
										is_BO_down_swing2 = true;
									}
								}
							}
							else
							{
								if (min_low_swingLow2 > iSwing2.SwingLow[0] - TicksToBO * TickSize)
								{					
									Draw.Dot(this, @"MaroonDot" + CurrentBar, true, 0, iSwing2.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Maroon);	
//									isActiveShortPosition = Sell("PendingOrder", iSwing2.SwingLow);
								}
								else
								{
									is_BO_down_swing2 = true;
								}
							}
						} 
						else if (iSwing2.SwingLow[0] <= ReferenceSMA[0])
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing2.SwingLow, ReferenceSMA, min_low_swingLow2, ExtremeLevelBar, 0, "Down", "Swing14", is_BO_down_swing2);
							is_BO_down_swing2 = ReturnedValues1.Item1;
							isActiveShortPosition = ReturnedValues1.Item2;
						}
						else
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(ReferenceSMA, ReferenceSMA, min_low_swingLow2, ExtremeLevelBar, ExtremeLevelBar, "Down", "Swing14", is_BO_down_swing2);
							is_BO_down_swing2 = ReturnedValues1.Item1;
							isActiveShortPosition = ReturnedValues1.Item2;
						}
					}
				}
				if (is_BO_down_swing2)
				{
					isSwingLow14 = false;
				}
				return new Tuple<bool, bool, bool>(isSwingLow14, isActiveShortPosition, is_BO_down_swing2);
			}
			return new Tuple<bool, bool, bool>(false, false, false);
		}

////	BOSwing14 proof when market order oportunity				
		public Tuple<bool, bool> BOProofSwing14(ISeries<double> ReferenceBOLevel, ISeries<double> ReferenceSMA, double ExtremeLevel, int ReferenceBar, int ReferenceSMABOBar, string Type, string Swing, bool isBO) 
		{
////		UP
			bool isActiveLongPosition = false;
			bool isActiveShortPosition = false;
			if (Type == "Up")
			{			
				if (!isBO)
				{								
					if (High[0] >= ReferenceBOLevel[0] + TicksToBO * TickSize)
					{
						if (ReferenceBar == 0)
						{
							Draw.Square(this, @"GreenSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] + 3 * TicksToBO * TickSize, Brushes.Green);
							isBO = true;
							if (Close[0] >= ReferenceBOLevel[0] + TicksToBO * TickSize)
							{
								if (Close[0] - ReferenceSMA[0] <= DistanceToSMA/* && Close[0] - ExtremeClose <= DistanceToPullbackClose*/)
								{
									isActiveLongPosition = Buy("MarketOrder", Close);
								}
							}
						}
						else
						{
							isBO = true;
						}
					}
					else
					{
						if (ExtremeLevel <= ReferenceBOLevel[ReferenceSMABOBar])
						{
							if (ExtremeLevel >= ReferenceBOLevel[ReferenceSMABOBar] + TicksToBO * TickSize)
							{							
								isBO = true;
							}
						}
						else
						{
							if (ExtremeLevel >= ReferenceBOLevel[0] + TicksToBO * TickSize)
							{							
								isBO = true;
							}
						}
					}
					if (!isBO)
					{
						Draw.Square(this, @"GreenSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] + 3 * TicksToBO * TickSize, Brushes.Green);
					}
				}
				else
				{
					if (ExtremeLevel <= iSwing2.SwingHigh[0])
					{
						isBO = false;	
					}
					else 
					{
						if (Swing == "Swing4")
						{
							if (Close[0] >= last_max_high_swingHigh1 + TicksToBO * TickSize)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, last_max_high_swingHigh1 + 3 * TicksToBO * TickSize, Brushes.Cyan);
								if (Close[0] - ReferenceSMA[0] <= DistanceToSMA/* && Close[0] - ExtremeClose <= DistanceToPullbackClose*/)
								{
									isActiveLongPosition = Buy("MarketOrder", Close);
								}
							}
						}
						else if (Swing == "Swing14")
						{
							if (Close[0] >= last_max_high_swingHigh2 + TicksToBO * TickSize && ReferenceBar == 0)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, last_max_high_swingHigh2 + 3 * TicksToBO * TickSize, Brushes.Cyan);
								if (Close[0] - ReferenceSMA[0] <= DistanceToSMA/* && Close[0] - ExtremeClose <= DistanceToPullbackClose*/)
								{
									isActiveLongPosition = Buy("MarketOrder", Close);
								}
							}
						}
					}					
				}
				return new Tuple<bool, bool> (isBO, isActiveLongPosition);
			}
			
////		DOWN
			else if (Type == "Down")
			{				
				if (!isBO)
				{	
					if (Low[0] <= ReferenceBOLevel[0] - TicksToBO * TickSize)
					{	
						if (ReferenceBar == 0)
						{
							Draw.Square(this, @"MaroonSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] - 3 * TicksToBO * TickSize, Brushes.Maroon);
							isBO = true;
							if (Close[0] <= ReferenceBOLevel[0] - TicksToBO * TickSize)
							{										
								if (ReferenceSMA[0] - Close[0] <= DistanceToSMA/* && ExtremeClose - Close[0] <= DistanceToPullbackClose*/)
								{
//									isActiveShortPosition = Sell("MarketOrder", Close);
								}
							}
						}
						else
						{
							isBO = true;
						}
					}
					else
					{
						if (ExtremeLevel >= ReferenceBOLevel[ReferenceSMABOBar])
						{
							if (ExtremeLevel <= ReferenceBOLevel[ReferenceSMABOBar] - TicksToBO * TickSize)
							{
								isBO = true;
							}
						}
						else
						{
							if (ExtremeLevel <= ReferenceBOLevel[0] - TicksToBO * TickSize)
							{
								isBO = true;
							}
						}
					}
					if (!isBO)
					{
						Draw.Square(this, @"MaroonSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] - 3 * TicksToBO * TickSize, Brushes.Maroon);
					}
				}
				else
				{
					if (ExtremeLevel >= iSwing2.SwingLow[0])
					{
						isBO = false;
					}
					else
					{
						if (Swing == "Swing4")
						{
							if (Close[0] <= last_min_low_swingLow1 - TicksToBO * TickSize)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, last_min_low_swingLow1 - 3 * TicksToBO * TickSize, Brushes.Cyan);
								if (ReferenceSMA[0] - Close[0] <= DistanceToSMA/* && ExtremeClose - Close[0] <= DistanceToPullbackClose*/)
								{
//									isActiveShortPosition = Sell("MarketOrder", Close);
								}
							}
						}
						else if (Swing == "Swing14")
						{
							if (Close[0] <= last_min_low_swingLow2 - TicksToBO * TickSize && ReferenceBar == 0)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, last_min_low_swingLow2 - 3 * TicksToBO * TickSize, Brushes.Cyan);
								if (ReferenceSMA[0] - Close[0] <= DistanceToSMA/* && ExtremeClose - Close[0] <= DistanceToPullbackClose*/)
								{
//									isActiveShortPosition = Sell("MarketOrder", Close);
								}
							}
						}
					}
				}
				return new Tuple<bool, bool> (isBO, isActiveShortPosition);
			}
			return new Tuple<bool, bool> (isBO, isActiveLongPosition);
		}
		
////	BO Swing4 proof when market order oportunity				
		public Tuple<bool, bool> BOProofSwing4(ISeries<double> ReferenceBOLevel, ISeries<double> ReferenceSMA, int ReferenceBar, int ReferenceSMABOBar, string Type) 
		{
////		UP
			bool isActiveLongPosition = false;
			bool isActiveShortPosition = false;
			if (Type == "Up")
			{		
				if (!is_BO_up_swing1)
				{								
					if (High[0] >= ReferenceBOLevel[0] + TicksToBO * TickSize)
					{
						if (ReferenceBar == 0)
						{
							Draw.Square(this, @"LimeSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
							is_BO_up_swing1 = true;
							if (Close[0] >= ReferenceBOLevel[0] + TicksToBO * TickSize)
							{
								if (iSwing2.SwingHigh[0] + TicksToBO * TickSize < Close[0])
								{
									if (Close[0] - ReferenceSMA[0] <= DistanceToSMA/* && Close[0] - ExtremeClose <= DistanceToPullbackClose*/)
									{
										isActiveLongPosition = Buy("MarketOrder", Close);
									}
								}
								else
								{
									if (Close[0] - ReferenceSMA[0] <= DistanceToSMA && iSwing2.SwingHigh[0] - Close[0] > iATR[0] * ClosnessFactor/* && Close[0] - ExtremeClose <= DistanceToPullbackClose*/)
									{
										isActiveLongPosition = Buy("MarketOrder", Close);
									}
								}									
							}
						}
						else
						{
							is_BO_up_swing1 = true;
						}
					}
					else
					{
						if (max_high_swingHigh1 <= ReferenceBOLevel[ReferenceSMABOBar])
						{
							if (max_high_swingHigh1 >= ReferenceBOLevel[ReferenceSMABOBar] + TicksToBO * TickSize)
							{							
								is_BO_up_swing1 = true;
							}
						}
						else 
						{
							if (max_high_swingHigh1 >= ReferenceBOLevel[0] + TicksToBO * TickSize)
							{							
								is_BO_up_swing1 = true;
							}
						}
					}
					if (!is_BO_up_swing1)
					{
						Draw.Square(this, @"LimeSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
					}
				}
				else
				{
					if (max_high_swingHigh1 <= iSwing1.SwingHigh[0])
					{
						is_BO_up_swing1 = false;	
					}
					else if (Close[0] >= last_max_high_swingHigh1 + TicksToBO * TickSize && ReferenceBar == 0)
					{
						Draw.Dot(this, @"CyanSquare" + CurrentBar, true, 0, last_max_high_swingHigh1 + 3 * TicksToBO * TickSize, Brushes.Cyan);
						if (iSwing2.SwingHigh[0] + TicksToBO * TickSize < Close[0])
						{
							if (Close[0] - ReferenceSMA[0] <= DistanceToSMA/* && Close[0] - ExtremeClose <= DistanceToPullbackClose*/)
							{
								isActiveLongPosition = Buy("MarketOrder", Close);
							}
						}
						else
						{
							if (Close[0] - ReferenceSMA[0] <= DistanceToSMA && iSwing2.SwingHigh[0] - Close[0] > iATR[0] * ClosnessFactor/* && Close[0] - ExtremeClose <= DistanceToPullbackClose*/)
							{
								isActiveLongPosition = Buy("MarketOrder", Close);
							}
						}									
					}
				}
				return new Tuple<bool, bool> (is_BO_up_swing1, isActiveLongPosition);
			}
			
////		DOWN
			else if (Type == "Down")
			{				
				if (!is_BO_down_swing1)
				{						
					if (Low[0] <= ReferenceBOLevel[0] - TicksToBO * TickSize)
					{	
						if (ReferenceBar == 0)
						{
							Draw.Square(this, @"RedSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] - 3 * TicksToBO * TickSize, Brushes.Red);
							is_BO_down_swing1 = true;
							if (Close[0] <= ReferenceBOLevel[0] - TicksToBO * TickSize)
							{	
								if (iSwing2.SwingLow[0] > Close[0])
								{
									if (ReferenceSMA[0] - Close[0] <= DistanceToSMA/* && ExtremeClose - Close[0] <= DistanceToPullbackClose*/)
									{
//										isActiveShortPosition = Sell("MarketOrder", Close);
									}
								}
								else
								{
									if (ReferenceSMA[0] - Close[0] <= DistanceToSMA && Close[0] - iSwing2.SwingLow[0] > iATR[0] * ClosnessFactor/* && ExtremeClose - Close[0] <= DistanceToPullbackClose*/)
									{
//										isActiveShortPosition = Sell("MarketOrder", Close);
									}
								}
							}
						}
						else
						{
							is_BO_down_swing1 = true;
						}
					}
					else
					{
						if (min_low_swingLow1 >= ReferenceBOLevel[ReferenceSMABOBar])
						{
							if (min_low_swingLow1 <= ReferenceBOLevel[ReferenceSMABOBar] - TicksToBO * TickSize)
							{
								is_BO_down_swing1 = true;
							}
						}
						else
						{
							if (min_low_swingLow1 <= ReferenceBOLevel[0] - TicksToBO * TickSize)
							{
								is_BO_down_swing1 = true;
							}
						}
					}
					if (!is_BO_down_swing1)
					{
						Draw.Square(this, @"RedSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] - 3 * TicksToBO * TickSize, Brushes.Red);
					}
				}
				else
				{
					if (min_low_swingLow1 >= iSwing1.SwingLow[0])
					{
						is_BO_down_swing1 = false;
					}
					else if (Close[0] <= last_min_low_swingLow1 - TicksToBO * TickSize && ReferenceBar == 0)
					{
						Draw.Dot(this, @"CyanSquare" + CurrentBar, true, 0, last_min_low_swingLow1 - 3 * TicksToBO * TickSize, Brushes.Indigo);
						if (iSwing2.SwingLow[0] > Close[0])
						{
							if (ReferenceSMA[0] - Close[0] <= DistanceToSMA/* && ExtremeClose - Close[0] <= DistanceToPullbackClose*/)
							{
//								isActiveShortPosition = Sell("MarketOrder", Close);
							}
						}
						else
						{
							if (ReferenceSMA[0] - Close[0] <= DistanceToSMA && Close[0] - iSwing2.SwingLow[0] > iATR[0] * ClosnessFactor/* && ExtremeClose - Close[0] <= DistanceToPullbackClose*/)
							{
//								isActiveShortPosition = Sell("MarketOrder", Close);
							}
						}
					}
				}
				return new Tuple<bool, bool> (is_BO_down_swing1, isActiveShortPosition);
			}
			return new Tuple<bool, bool> (is_BO_up_swing1, isActiveLongPosition);
		}

		
////	Market order submission when there is an opposing peinding order				
		public Tuple<bool, bool> MarketVSPending(ISeries<double> ReferenceBOLevel, ISeries<double> ReferenceSwing, ISeries<double> ReferenceSMA, int ReferenceBar, bool isBO, double ExtremeLevel, string Type) 
		{
////		UP
			bool isActiveLongPosition = false;
			bool isActiveShortPosition = false;
			if (Type == "Up")
			{	
				if (!isBO)
				{													
					if (High[0] >= ReferenceBOLevel[0] + TicksToBO * TickSize)
					{						
						if (ReferenceBar == 0)
						{
							Draw.Square(this, @"WhiteSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] + 3 * TicksToBO * TickSize, Brushes.White);
							isBO = true;
							if (Close[0] - ReferenceSMA[0] <= DistanceToSMA/* && Close[0] - ExtremeClose <= DistanceToPullbackClose*/)
							{
								isActiveLongPosition = Buy("MarketOrder", Close);
							}
						}
						else
						{
							isBO = true;
						}
					}
					else
					{
						if (ExtremeLevel >= ReferenceBOLevel[0] + TicksToBO * TickSize)
						{							
							isBO = true;
						}
					}
					if (!isBO)
					{
						Draw.Square(this, @"WhiteSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] + 3 * TicksToBO * TickSize, Brushes.White);
					}
				}
				else
				{
					if (ExtremeLevel <= ReferenceSwing[0])
					{
						isBO = false;	
					}
				}
				return new Tuple<bool, bool> (isBO, isActiveLongPosition);
			}
			
////		DOWN
			else if (Type == "Down")
			{				
				if (!isBO)
				{						
					if (Low[0] <= ReferenceBOLevel[0] - TicksToBO * TickSize)
					{	
						if (ReferenceBar == 0)
						{
							Draw.Square(this, @"BlackSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] - 3 * TicksToBO * TickSize, Brushes.Black);
							isBO = true;									
							if (ReferenceSMA[0] - Close[0] <= DistanceToSMA/* && ExtremeClose - Close[0] <= DistanceToPullbackClose*/)
							{
//								isActiveShortPosition = Sell("MarketOrder", Close);
							}
						}
						else
						{
							isBO = true;
						}
					}
					else
					{
						if (ExtremeLevel <= ReferenceBOLevel[0] - TicksToBO * TickSize)
						{
							isBO = true;
						}
					}
					if (!isBO)
					{
						Draw.Square(this, @"BlackSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] - 3 * TicksToBO * TickSize, Brushes.Black);
					}
				}
				else
				{
					if (ExtremeLevel >= ReferenceSwing[0])
					{
						isBO = false;
					}
				}
				return new Tuple<bool, bool> (isBO, isActiveShortPosition);
			}
			return new Tuple<bool, bool> (isBO, isActiveLongPosition);
		}		
	}
    #endregion
}
