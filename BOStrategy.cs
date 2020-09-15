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
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class BOStrategy : Strategy
	{
		#region Variables
		private SMA iSMA1, iSMA2, iSMA3;
		private ATR iATR;
		private Swing iSwing4, iSwing14;
		private ArrayList LastSwingHigh14Cache, LastSwingLow14Cache;
		private int cross_above_bar, cross_below_bar, amount_long, fix_amount_long, amount_short, fix_amount_short;
		private double ATR_crossing_value, SMA_dis, stop_price_long, trigger_price_long, stop_price_short, trigger_price_short, swingHigh14_max, swingLow14_min, MaxCloseSwingLow4, RangeSizeSwingLow4, MaxCloseSwingLow14;
		private double last_max_high_swingHigh4, last_max_high_swingHigh14, last_min_low_swingLow4, last_min_low_swingLow14;
		private double RangeSizeSwingLow14, MinCloseSwingHigh4, RangeSizeSwingHigh4, MinCloseSwingHigh14, RangeSizeSwingHigh14, swingHigh14_max_reentry, swingLow14_min_reentry, max_high_swingHigh4, max_high_swingHigh14, min_low_swingLow4, min_low_swingLow14;
		private double current_swingHigh14, current_swingLow14, current_swingHigh4, current_swingLow4, current_ATR, current_stop, CurrentOpen, CurrentClose, CurrentHigh, CurrentLow, fix_stop_price_long, fix_trigger_price_long, fix_stop_price_short, fix_trigger_price_short;
		private bool is_incipient_up_trend, is_incipient_down_trend, is_upward = true, is_downward = true, gray_ellipse_long, gray_ellipse_short, is_reentry_long, is_reentry_short, is_long, is_short, isBO, is_BO_up_swing4, is_BO_up_swing14, is_BO_down_swing4, is_BO_down_swing14;
		private bool cross_below_20_to_50, cross_above_20_to_50;
		private Account myAccount;
		private Order my_entry_order = null, my_entry_market = null, my_exit_order = null;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Strategy that Operates Breakouts";
				Name = "BOStrategy";
				Calculate = Calculate.OnBarClose;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = false;
				ExitOnSessionCloseSeconds = 30;
				IsFillLimitOnTouch = false;
				MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution = OrderFillResolution.Standard;
				Slippage = 0;
				StartBehavior = StartBehavior.WaitUntilFlat;
				TimeInForce = TimeInForce.Gtc;
				TraceOrders = true;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade = 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration = true;
				SMA1 = 200;
				SMA2 = 50;
				SMA3 = 20;
				ATR1 = 100;
				Swing1 = 4;
				Swing2 = 14;
				UnitsTriggerForTrailing = 1;
				TrailingUnitsStop = 2;
				RiskUnit = 420;
				IncipientTrandFactor = 2;
				ATRStopFactor = 3;
				TicksToBO = 10;
				SwingPenetration = 50;
				ClosnessFactor = 2;
				ClosnessToTrade = 1;
				MagicNumber = 50;
			}
			else if (State == State.Configure)
			{
				AddDataSeries("GBPUSD", Data.BarsPeriodType.Minute, 60, Data.MarketDataType.Last);
			}
			else if (State == State.DataLoaded)
			{

				// iSMA1 > iSMA2 > iSMA3
				iSMA1 = SMA(Close, SMA1);
				iSMA2 = SMA(Close, SMA2);
				iSMA3 = SMA(Close, SMA3);

				iATR = ATR(Close, ATR1);

				iSwing4 = Swing(Close, Swing1);
				iSwing14 = Swing(Close, Swing2);

				iSMA1.Plots[0].Brush = Brushes.Red;
				iSMA2.Plots[0].Brush = Brushes.Gold;
				iSMA3.Plots[0].Brush = Brushes.Lime;
				iATR.Plots[0].Brush = Brushes.White;
				iSwing4.Plots[0].Brush = Brushes.Fuchsia;
				iSwing4.Plots[1].Brush = Brushes.Gold;
				iSwing14.Plots[0].Brush = Brushes.Silver;
				iSwing14.Plots[1].Brush = Brushes.Silver;
				AddChartIndicator(iSMA1);
				AddChartIndicator(iSMA2);
				AddChartIndicator(iSMA3);
				AddChartIndicator(iATR);
				AddChartIndicator(iSwing4);
				AddChartIndicator(iSwing14);
				LastSwingHigh14Cache = new ArrayList();
				LastSwingLow14Cache = new ArrayList();
			}
		}

		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled,
			double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{
			// Assign entryOrder in OnOrderUpdate() to ensure the assignment occurs when expected.
			// This is more reliable than assigning Order objects in OnBarUpdate, as the assignment is not gauranteed to be complete if it is referenced immediately after submitting
			if (order.Name == "entryOrder")
			{
				my_entry_order = order;
			}
			if (order.Name == "entryMarket")
			{
				my_entry_market = order;
			}
			if (order.Name == "exit")
			{
				my_exit_order = order;
			}
		}

		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			// Reset our stop order and target orders' Order objects after our position is closed. (1st Entry)
			if (my_exit_order != null)
			{
				if (my_exit_order == execution.Order)
				{
					if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled)
					{
						if (my_entry_order != null)
						{
							my_entry_order.OrderState = OrderState.Cancelled;
						}
						if (my_entry_market != null)
						{
							my_entry_market.OrderState = OrderState.Cancelled;
						}
					}
				}
			}
			if (my_entry_market != null)
			{
				if (my_entry_market == execution.Order)
				{
					if (execution.Order.OrderState == OrderState.Filled || execution.Order.OrderState == OrderState.PartFilled)
					{
						if (my_entry_order != null)
						{
							if (my_entry_order.OrderState == OrderState.Filled)
							{
								my_entry_order.OrderState = OrderState.Cancelled;
							}
						}
					}
				}
			}
		}

		protected override void OnBarUpdate()
		{
			#region Chart_Initialization
			if (BarsInProgress != 0)
				return;

			if (CurrentBars[0] < 0)
				return;

			if (CurrentBar < BarsRequiredToTrade)
				return;

			//This conditional checks that the indicator values that will be used in later calculations are not equal to 0.
			if (iSwing14.SwingHigh[0] == 0 || iSwing14.SwingLow[0] == 0 || iSwing4.SwingHigh[0] == 0 || iSwing4.SwingLow[0] == 0 || iATR[0] == 0)
				return;
            #endregion

            #region Variable_Reset
            ////		Reset isBO variable once a new swing comes up	
            if (current_swingHigh4 != iSwing4.SwingHigh[0])
			{
				is_BO_up_swing4 = false;
			}
			if (current_swingHigh14 != iSwing14.SwingHigh[0])
			{
				is_BO_up_swing14 = false;
			}
			if (current_swingLow4 != iSwing4.SwingLow[0])
			{
				is_BO_down_swing4 = false;
			}
			if (current_swingLow14 != iSwing14.SwingLow[0])
			{
				is_BO_down_swing14 = false;
			}

			last_max_high_swingHigh4 = max_high_swingHigh4;
			last_max_high_swingHigh14 = max_high_swingHigh14;
			last_min_low_swingLow4 = min_low_swingLow4;
			last_min_low_swingLow14 = min_low_swingLow14;

			is_reentry_long = false;
			is_reentry_short = false;

			////		is_long, is_short reseting 		
			if (Position.MarketPosition == MarketPosition.Flat) //if the postition is stil active and the initial stop and trailing stop trigger price levels were set then...
			{
				is_long = false;
				is_short = false;
			}
            #endregion

            #region Variable_Initialization       
            current_swingHigh4 = iSwing4.SwingHigh[0];
			current_swingLow4 = iSwing4.SwingLow[0];
			current_swingHigh14 = iSwing14.SwingHigh[0];
			current_swingLow14 = iSwing14.SwingLow[0];
			current_ATR = iATR[0];
			current_stop = current_ATR * ATRStopFactor;
			CurrentOpen = Open[0];
			CurrentClose = Close[0];
			CurrentHigh = High[0];
			CurrentLow = Low[0];
            #endregion

            #region Parameters_Check
            //This block of code checks if the indicator values that will be used in later calculations are correct.
            //When a SMA of period 200 prints a value in the bar 100 is an example of a wrong indicator value.
            {
                //Create an array so that we can iterate through the values.
                int[] indicators = new int[4];
				indicators[0] = SMA1;
				indicators[1] = SMA2;
				indicators[2] = SMA3;
				indicators[3] = ATR1;

				//Find the max indicator calculation value.
				int max_indicator_bar_calculation = 0;
				for (int i = 0; i < 4; i++)
				{
					if (indicators[i] > max_indicator_bar_calculation)
					{
						max_indicator_bar_calculation = indicators[i];
					}
				}

				//Stop calculations if the chart is not printing correct indicator values.
				if (CurrentBar < max_indicator_bar_calculation)
				{
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

			//This condition evaluates if the SMAs parameters are set incorrectly or unsorted.
			//The SMAs values have to be set like the following: SMA1 > SMA2 > SMA3.
			//This block of code sorts (with Bubble Sort) and sets sorted values to the SMAs. 
			if (SMA1 < SMA2 || SMA1 < SMA3 || SMA2 < SMA3)
			{
				//Create an array so that we can iterate through the values.
				int[] SMAv = new int[3];
				SMAv[0] = SMA1;
				SMAv[1] = SMA2;
				SMAv[2] = SMA3;

				//Sort using the Bubble Sort algorithm.
				bool isSorted = false;
				while (!isSorted)
				{
					isSorted = true;
					for (int i = 0; i < 2; i++)
					{
						if (SMAv[i] < SMAv[i + 1])
						{
							int tmp = SMAv[i];
							SMAv[i] = SMAv[i + 1];
							SMAv[i + 1] = tmp;
							isSorted = false;
						}
					}
				}

				//Asign sorted values to the SMAs indicators.
				iSMA1 = SMA(Close, SMAv[0]);
				iSMA2 = SMA(Close, SMAv[1]);
				iSMA3 = SMA(Close, SMAv[2]);

				//This reassignment is done to avoid this conditional to be executed again.
				SMA1 = SMAv[0];
				SMA2 = SMAv[1];
				SMA3 = SMAv[2];

				Print(string.Format("The SMAs have been sorted as follow: iSMA1: {0} // iSMA2: {1} // iSMA3: {2}", SMAv[0], SMAv[1], SMAv[2]));
			}

            #endregion

            #region Overall_Market_Movement
            ////		SMA 50-200 Distance Method Calling 	
            SMA_dis = Math.Abs(iSMA1[0] - iSMA2[0]); ; //Calculates the Distance Between the SMA 50-200			

			////		Identifying the overall market  movement type Process and saving the ATR value at that time (SMA 50-200 Crossing Event) (if the SMA 50 is above the SMA 200 then there is an upward overall movement type, if the SMA 50 is below the SMA 200 then there is an downward overall movement type)	
			///Above
			if (CrossAbove(iSMA2, iSMA1, 1) && is_downward) //Once an SMAs 50-200 crossabove happens and there exists a downward overall movement then...
			{
				ATR_crossing_value = iATR[0]; //Tomar valor del ATR en el cruce
				is_upward = true; //Defining overall movement type
				is_downward = false; //Opposing movement Flag Lowering
				cross_above_bar = CurrentBar; //Saves the bar number at the crossing time
			}

			///Below
			else if (CrossBelow(iSMA2, iSMA1, 1) && is_upward) //Once an SMAs 50-200 crossbelow happens and there exists an upward overall movement then...
			{
				ATR_crossing_value = iATR[0]; //Tomar valor ATR en el cruce
				is_downward = true; //Defining overall movement type
				is_upward = false; //Opposing movement Flag Lowering
				cross_below_bar = CurrentBar; //Saves the bar number at the crossing time
			}
            #endregion

            #region Incipient_Trend_Identification
            ////		IncipientTrend Identification Process (IncipientTrend: there has been an SMAs 50-200 crossing event and the distance between those SMAs has reached the double of the ATR value at the crossing time)
            ///UpWard			
            if (is_upward && SMA_dis >= IncipientTrandFactor * ATR_crossing_value && ATR_crossing_value != 0) //Once an upward overall movement has been identified, the SMAs 50-200 distance is greater than the ATR value at the crossing time and there have been a first CrossAbove 50-200 event that records an ATR value then...
			{
				is_incipient_up_trend = true; //is_incipient_up_trend turning on
				is_incipient_down_trend = false; //is_incipient_down_trend turning off
				gray_ellipse_short = false; // gray_ellipse_short Flag Lowering
			}

			///DownWard		
			else if (is_downward && SMA_dis >= IncipientTrandFactor * ATR_crossing_value && ATR_crossing_value != 0) //Once an downward overall movement has been identified, the SMAs 50-200 distance is greater than the ATR value at the crossing time and there have been a first CrossBelow 50-200 event that records an ATR value then...
			{
				is_incipient_down_trend = true; //is_incipient_down_trend turning on
				is_incipient_up_trend = false; //is_incipient_up_trend turning off
				gray_ellipse_long = false; //gray_ellipse_long Flag Lowering
			}
            #endregion

            #region Gray_Ellipse
            ////		GrayEllipse turn on/off inside an incipient trend (SMA 20-50 Crossing Event) (GrayEllipse: An SMAs 20-50 crossing event in the opposite direction of the overall market movement)
            #region Long
            ///Long
            if (CrossBelow(iSMA3, iSMA2, 1))
			{
				cross_below_20_to_50 = true;
			}

			if (is_incipient_up_trend && cross_below_20_to_50) // If the SMA 50 is above the SMA200 (upward market movement), there is an SMAs 20-50 crossbelow event and the GrayEllipse in that direction is turned off (false) then...
			{
				gray_ellipse_long = true; //gray_ellipse_long Flag turning on

				int ArrayListSize = CurrentBar - cross_above_bar; //Calculating the distance in bars (that determines the array size to check for the max/min swinghigh/low level) between the SMAs 20-50 crossing event and the Last SMAs 50-200 crossing event			
				if (ArrayListSize >= 240) //trick to avoid the bug when trying to find the value of swing indicator beyond the 256 MaximunBarlookbar period, which is not possible
				{
					ArrayListSize = 240;
				}

				swingHigh14_max = iSwing14.SwingHigh[0]; //initializing the variable that is going to keep the reference high, with the array firts value for comparison purposes
				for (int i = 0; i <= ArrayListSize; i++) //Loop that walk the array 
				{
					if (iSwing14.SwingHigh[i] > swingHigh14_max && iSwing14.SwingHigh[i] != 0) //To determine the highest value (highest swinghigh 14)
					{
						swingHigh14_max = iSwing14.SwingHigh[i]; //And saves that value in this variable
					}
				}

				cross_below_20_to_50 = false;
			}
			else if (iSwing14.SwingHigh[0] > swingHigh14_max && gray_ellipse_long) //if the high of the current bar surpass the Max Level of the swinghigh (strength 14) where the gray_ellipse_long was originated 
			{
				swingHigh14_max_reentry = iSwing14.SwingHigh[0]; //Then lower the flag of the GrayEllipse in that direction
				is_reentry_long = true;
			}

			if (High[0] > swingHigh14_max_reentry && is_reentry_long) //if the high of the current bar surpass the Max Level of the swinghigh (strength 14) where the gray_ellipse_long was originated 
			{
				gray_ellipse_long = false; //Then lower the flag of the GrayEllipse in that direction
				is_reentry_long = false;
			}
            #endregion

            #region Short
            ///Short
            if (CrossAbove(iSMA3, iSMA2, 1))
			{
				cross_above_20_to_50 = true;
			}

			if (is_incipient_down_trend && cross_above_20_to_50/* && !gray_ellipse_short*/) // If the SMA 50 is below the SMA200 (downward market movement), there is an SMAs 20-50 crabove event and the GrayEllipse in that direction is turned off (false) then...
			{
				gray_ellipse_short = true; //gray_ellipse_long Flag turning on

				int ArrayListSize = CurrentBar - cross_below_bar; //Calculating the distance in bars (that determines the array size to check for the max/min swinghigh/low level) between the SMAs 20-50 crossing event and the Last SMAs 50-200 crossing event			
				if (ArrayListSize >= 240) //trick to avoid the bug when trying to find the value of swing indicator beyond the 256 MaximunBarlookbar period, which is not possible
				{
					ArrayListSize = 240;
				}

				swingLow14_min = iSwing14.SwingLow[0]; //initializing the variable that is going to keep the reference low, with the array firts value for comparison purposes
				for (int i = 0; i <= ArrayListSize; i++) //Loop that walk the array
				{
					if (iSwing14.SwingLow[i] < swingLow14_min && iSwing14.SwingLow[i] != 0) //To determine the lowest value (lowest swinglow 14)
					{
						swingLow14_min = iSwing14.SwingLow[i]; //And saves that value in this variable
					}
				}

				cross_above_20_to_50 = false;
			}
			else if (iSwing14.SwingLow[0] < swingLow14_min && gray_ellipse_short) //if the Low of the current bar surpass the Min Level of the swinglow (strength 14) where the gray_ellipse_short was originated
			{
				swingLow14_min_reentry = iSwing14.SwingLow[0]; //Then lower the flag of the GrayEllipse in that direction
				is_reentry_short = true;
			}

			if (Low[0] < swingLow14_min_reentry && is_reentry_short) //if the Low of the current bar surpass the Min Level of the swinglow (strength 14) where the gray_ellipse_short was originated
			{
				gray_ellipse_short = false; //Then lower the flag of the GrayEllipse in that direction
				is_reentry_short = false;
			}
            #endregion
            #endregion

            #region Trade_Identification
            ////		TRADE IDENTIFICATION			
            ////		Red Trade Type Process
            #region Long
            ///Long			 
            if (is_incipient_down_trend && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)) //if the overall market movement is upward and there is no active position then...
			{
				///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4				
				bool isSwingHigh4 = false; //Higher Swing strength 4 Flag creation, set to false and pending of validation
				bool isActiveLongPosition = false;
				if (iSwing4.SwingHigh[0] >= iSMA1[0] - iATR[0] * ClosnessToTrade && ((iSwing4.SwingHigh[0] + TicksToBO * TickSize) - iSMA1[0] <= current_stop || (iSwing4.SwingHigh[0] + TicksToBO * TickSize) - iSMA2[0] <= current_stop)) // If the reference Swing High 4 is above the SMA 50 then...
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA1, "High");
					isSwingHigh4 = ReturnedValues.Item1;
					isActiveLongPosition = ReturnedValues.Item2;
					is_BO_up_swing4 = ReturnedValues.Item3;
				}

				///Validate whether there is a valid higher low strength 14 (HL14) in case there is no valid HL4 and if so then send a long stop order above the reference swing high 14				
				if (iSwing14.SwingHigh[0] >= iSMA1[0] - iATR[0] * ClosnessToTrade && !isSwingHigh4 && !isActiveLongPosition && ((iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA1[0] <= current_stop || (iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA2[0] <= current_stop)) // If the reference Swing High 14 is above the SMA 50 and there is no HL4 then...
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA1, "High");
					bool isSwingHigh14 = ReturnedValues.Item1;
					isActiveLongPosition = ReturnedValues.Item2;
					is_BO_up_swing14 = ReturnedValues.Item3;
				}
			}
            #endregion

            #region Short
            ///Short		 
            else if (is_incipient_up_trend && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)) //if the overall market movement is upward and there is no active position then...
			{
				///Validate whether there is a valid lower high strength 4 (LH4) and if so then send a short stop order below the reference swing low 4			
				bool isSwingLow4 = false; //Lower Swing strength 4 Flag creation, set to false and pending of validation
				bool isActiveShortPosition = false;
				if (iSwing4.SwingLow[0] <= iSMA1[0] + iATR[0] * ClosnessToTrade && (iSMA1[0] - (iSwing4.SwingLow[0] - TicksToBO * TickSize) <= current_stop || iSMA2[0] - (iSwing4.SwingLow[0] - TicksToBO * TickSize) <= current_stop))
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA1, "Low");
					isSwingLow4 = ReturnedValues.Item1;
					isActiveShortPosition = ReturnedValues.Item2;
					is_BO_down_swing4 = ReturnedValues.Item3;
				}

				///Validate whether there is a valid lower high strength 14 (LH14) in case there is no valid LH4 and if so then send a short stop order below the reference swing low 14			
				if (iSwing14.SwingLow[0] <= iSMA1[0] + iATR[0] * ClosnessToTrade && !isSwingLow4 && !isActiveShortPosition && (iSMA1[0] - (iSwing14.SwingLow[0] - TicksToBO * TickSize) <= current_stop || iSMA2[0] - (iSwing14.SwingLow[0] - TicksToBO * TickSize) <= current_stop))
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA1, "Low");
					bool isSwingLow14 = ReturnedValues.Item1;
					isActiveShortPosition = ReturnedValues.Item2;
					is_BO_down_swing14 = ReturnedValues.Item3;
				}
			}
            #endregion
            #endregion

            #region Traditional_Red
            ////		TRADITIONAL RED Trade Type Process	
            #region Long
            ///Long			 
            if (is_incipient_up_trend && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)) //if the overall market movement is upward and there is no active position then...
			{
				///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4				
				bool isSwingHigh4 = false; //Higher Swing strength 4 Flag creation, set to false and pending of validation
				bool isActiveLongPosition = false;
				if (iSwing4.SwingHigh[0] >= iSMA1[0] - iATR[0] * ClosnessToTrade && (iSwing4.SwingHigh[0] + TicksToBO * TickSize) - iSMA1[0] <= current_stop && iSMA2[0] - iSwing4.SwingHigh[0] > iATR[0] * ClosnessFactor) // If the reference Swing High 4 is above the SMA 50 then...
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA1, "High");
					isSwingHigh4 = ReturnedValues.Item1;
					isActiveLongPosition = ReturnedValues.Item2;
					is_BO_up_swing4 = ReturnedValues.Item3;
				}

				///Validate whether there is a valid higher low strength 14 (HL14) in case there is no valid HL4 and if so then send a long stop order above the reference swing high 14				
				if (iSwing14.SwingHigh[0] >= iSMA1[0] - iATR[0] * ClosnessToTrade && !isSwingHigh4 && !isActiveLongPosition && (iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA1[0] <= current_stop && iSMA2[0] - iSwing14.SwingHigh[0] > iATR[0] * ClosnessFactor) // If the reference Swing High 14 is above the SMA 50 and there is no HL4 then...
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA1, "High");
					bool isSwingHigh14 = ReturnedValues.Item1;
					isActiveLongPosition = ReturnedValues.Item2;
					is_BO_up_swing14 = ReturnedValues.Item3;
				}
			}
            #endregion

            #region Short
            ///Short		 
            if (is_incipient_down_trend && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)) //if the overall market movement is upward and there is no active position then...
			{
				///Validate whether there is a valid lower high strength 4 (LH4) and if so then send a short stop order below the reference swing low 4			
				bool isSwingLow4 = false; //Lower Swing strength 4 Flag creation, set to false and pending of validation
				bool isActiveShortPosition = false;
				if (iSwing4.SwingLow[0] <= iSMA1[0] + iATR[0] * ClosnessToTrade && iSMA1[0] - (iSwing4.SwingLow[0] - TicksToBO * TickSize) <= current_stop && iSwing4.SwingLow[0] - iSMA2[0] > iATR[0] * ClosnessFactor)
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA1, "Low");
					isSwingLow4 = ReturnedValues.Item1;
					isActiveShortPosition = ReturnedValues.Item2;
					is_BO_down_swing4 = ReturnedValues.Item3;
				}

				///Validate whether there is a valid lower high strength 14 (LH14) in case there is no valid LH4 and if so then send a short stop order below the reference swing low 14			
				if (iSwing14.SwingLow[0] <= iSMA1[0] + iATR[0] * ClosnessToTrade && !isSwingLow4 && !isActiveShortPosition && iSMA1[0] - (iSwing14.SwingLow[0] - TicksToBO * TickSize) <= current_stop && iSwing14.SwingLow[0] - iSMA2[0] > iATR[0] * ClosnessFactor)
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA1, "Low");
					bool isSwingLow14 = ReturnedValues.Item1;
					isActiveShortPosition = ReturnedValues.Item2;
					is_BO_down_swing14 = ReturnedValues.Item3;
				}
			}
            #endregion
            #endregion

            #region Traditional
            ////		Traditional Trade Type Process
            #region Long
            ///Long
            if (is_incipient_up_trend && gray_ellipse_long && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)) //if we have an is_incipient_up_trend, a gray_ellipse_long and there is no active position then...
			{
                #region Normal_Traditional
                ///Normal traditional
                if (is_upward)
				{
					///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4				
					bool isSwingHigh4 = false; //Higher Swing strength 4 Flag creation, set to false and pending of validation
					bool isActiveLongPosition = false;
					if (iSwing4.SwingHigh[0] >= iSMA2[0] - iATR[0] * ClosnessToTrade && (iSwing4.SwingHigh[0] + TicksToBO * TickSize) - iSMA2[0] <= current_stop) // If the reference Swing High 4 is above the SMA 50 then...
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA2, "High");
						isSwingHigh4 = ReturnedValues.Item1;
						isActiveLongPosition = ReturnedValues.Item2;
						is_BO_up_swing4 = ReturnedValues.Item3;
					}

					///Validate whether there is a valid higher low strength 14 (HL14) in case there is no valid HL4 and if so then send a long stop order above the reference swing high 14					
					if (iSwing14.SwingHigh[0] >= iSMA2[0] - iATR[0] * ClosnessToTrade && !isSwingHigh4 && !isActiveLongPosition && (iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA2[0] <= current_stop) // If the reference Swing High 14 is above the SMA 50 and there is no HL4 then...
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA2, "High");
						bool isSwingHigh14 = ReturnedValues.Item1;
						isActiveLongPosition = ReturnedValues.Item2;
						is_BO_up_swing14 = ReturnedValues.Item3;
					}
				}
                #endregion

                #region Modified_Traditional
                ///Modified traditional
                else
                {
					///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4			
					bool isSwingHigh4 = false; //Higher Swing strength 4 Flag creation, set to false and pending of validation
					bool isActiveLongPosition = false;
					if (iSwing4.SwingHigh[0] >= iSMA1[0] - iATR[0] * ClosnessToTrade && (iSwing4.SwingHigh[0] + TicksToBO * TickSize) - iSMA1[0] <= current_stop) // If the reference Swing High 4 is above the SMA 50 then...
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA1, "High");
						isSwingHigh4 = ReturnedValues.Item1;
						isActiveLongPosition = ReturnedValues.Item2;
						is_BO_up_swing4 = ReturnedValues.Item3;
					}

					///Validate whether there is a valid higher low strength 14 (HL14) in case there is no valid HL4 and if so then send a long stop order above the reference swing high 14				
					if (iSwing14.SwingHigh[0] >= iSMA1[0] - iATR[0] * ClosnessToTrade && !isSwingHigh4 && !isActiveLongPosition && (iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA1[0] <= current_stop) // If the reference Swing High 14 is above the SMA 50 and there is no HL4 then...
					{

						Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA1, "High");
						bool isSwingHigh14 = ReturnedValues.Item1;
						isActiveLongPosition = ReturnedValues.Item2;
						is_BO_up_swing14 = ReturnedValues.Item3;
					}
				}
                #endregion
            }
            #endregion

            #region Short
            ///Short	
            else if (is_incipient_down_trend && gray_ellipse_short && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)) //if we have an is_incipient_down_trend, a gray_ellipse_short and there is no active position then...
			{
                #region Normal_Traditional
                ///Normal traditional
                if (is_downward)
				{
					///Validate whether there is a valid lower high strength 4 (LH4) and if so then send a short stop order below the reference swing low 4		
					bool isSwingLow4 = false; //Lower Swing strength 4 Flag creation, set to false and pending of validation
					bool isActiveShortPosition = false;
					if (iSwing4.SwingLow[0] <= iSMA2[0] + iATR[0] * ClosnessToTrade && iSMA2[0] - (iSwing4.SwingLow[0] - TicksToBO * TickSize) <= current_stop)
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA2, "Low");
						isSwingLow4 = ReturnedValues.Item1;
						isActiveShortPosition = ReturnedValues.Item2;
						is_BO_down_swing4 = ReturnedValues.Item3;
					}
					///Validate whether there is a valid lower high strength 14 (LH14) in case there is no valid LH4 and if so then send a short stop order below the reference swing low 14				
					if (iSwing14.SwingLow[0] <= iSMA2[0] + iATR[0] * ClosnessToTrade && !isSwingLow4 && !isActiveShortPosition && iSMA2[0] - (iSwing14.SwingLow[0] - TicksToBO * TickSize) <= current_stop)
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA2, "Low");
						bool isSwingLow14 = ReturnedValues.Item1;
						isActiveShortPosition = ReturnedValues.Item2;
						is_BO_down_swing14 = ReturnedValues.Item3;
					}
				}
                #endregion

                #region Modified_Traditional
                ///Modified traditional
                else
                {
					///Validate whether there is a valid lower high strength 4 (LH4) and if so then send a short stop order below the reference swing low 4			
					bool isSwingLow4 = false; //Lower Swing strength 4 Flag creation, set to false and pending of validation
					bool isActiveShortPosition = false;
					if (iSwing4.SwingLow[0] <= iSMA1[0] + iATR[0] * ClosnessToTrade && iSMA1[0] - (iSwing4.SwingLow[0] - TicksToBO * TickSize) <= current_stop)
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA1, "Low");
						isSwingLow4 = ReturnedValues.Item1;
						isActiveShortPosition = ReturnedValues.Item2;
						is_BO_down_swing4 = ReturnedValues.Item3;
					}
					///Validate whether there is a valid lower high strength 14 (LH14) in case there is no valid LH4 and if so then send a short stop order below the reference swing low 14				
					if (iSwing14.SwingLow[0] <= iSMA1[0] + iATR[0] * ClosnessToTrade && !isSwingLow4 && !isActiveShortPosition && iSMA1[0] - (iSwing14.SwingLow[0] - TicksToBO * TickSize) <= current_stop)
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA1, "Low");
						bool isSwingLow14 = ReturnedValues.Item1;
						isActiveShortPosition = ReturnedValues.Item2;
						is_BO_down_swing14 = ReturnedValues.Item3;
					}
				}
                #endregion
            }
            #endregion
            #endregion

            #region Trade_Management
            ////		TRADE MANAGEMENT (Stop and Trailing Stop Trigger Setting)						
            ///Stop Updating Process (by both trailing and SMA)
			#region Long
            ///While Long	
            if (Position.MarketPosition == MarketPosition.Long && !is_long) //if the postition is stil active and the initial stop and trailing stop trigger price levels were set then...
			{
				is_long = true;
				is_short = false;
				fix_amount_long = amount_long;
				fix_stop_price_long = stop_price_long;
				fix_trigger_price_long = trigger_price_long;
				Draw.FibonacciRetracements(this, "tag1" + CurrentBar, false, 0, Position.AveragePrice, 10, fix_stop_price_long);
			}
			if (Position.MarketPosition == MarketPosition.Long && is_long) //if the postition is stil active and the initial stop and trailing stop trigger price levels were set then...
			{
				if (my_entry_order == null || my_entry_market == null)
				{
					if (my_entry_market == null && my_entry_order.OrderState == OrderState.Filled)
					{
						//						if (my_entry_order.OrderState == OrderState.Filled)
						//						{
						//							if (my_entry_order.OrderType == OrderType.StopMarket && my_entry_order.OrderState == OrderState.Working && my_entry_order.OrderAction == OrderAction.SellShort)
						//							{
						//								Print (String.Format("{0} // {1} // {2} // {3}", my_entry_order.StopPrice, fix_stop_price_long,  my_entry_order.Quantity, Time[0]));
						//							}
						//							else
						//							{
						ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryOrder");
						//							}
						//						}										
						if (High[BarsSinceEntryExecution(0, @"entryOrder", 0)] < iSMA2[BarsSinceEntryExecution(0, @"entryOrder", 0)] + TicksToBO * TickSize)
						{
							bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
							{
								if (High[i] >= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
									HighCrossEma50 = true; //And saves that value in this variable
									break;
								}
							}
							if (HighCrossEma50 == false)
							{
								if (Close[0] < iSMA1[0] - iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(fix_amount_long, @"exit", @"entryOrder");
								}
							}
							else
							{
								if (Close[0] < iSMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA1[0] - iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(fix_amount_long, @"exit", @"entryOrder");
								}
							}
						}
						else
						{
							if (Close[0] < iSMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA1[0] - iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(fix_amount_long, @"exit", @"entryOrder");
							}
						}
						if (High[0] >= fix_trigger_price_long)
						{
							fix_stop_price_long = High[0] - current_stop * TrailingUnitsStop;
							fix_trigger_price_long = High[0];
							ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryOrder");
							Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
						}
					}
					else if (my_entry_order == null && my_entry_market.OrderState == OrderState.Filled)
					{
						ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryMarket");
						//						}
						//					}				
						if (High[BarsSinceEntryExecution(0, @"entryMarket", 0)] < iSMA2[BarsSinceEntryExecution(0, @"entryMarket", 0)] + TicksToBO * TickSize)
						{
							bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
							{
								if (High[i] >= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
									HighCrossEma50 = true; //And saves that value in this variable
									break;
								}
							}
							if (HighCrossEma50 == false)
							{
								if (Close[0] < iSMA1[0] - iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(fix_amount_long, @"exit", @"entryMarket");
								}
							}
							else
							{
								if (Close[0] < iSMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA1[0] - iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(fix_amount_long, @"exit", @"entryMarket");
								}
							}
						}
						else
						{
							if (Close[0] < iSMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA1[0] - iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(fix_amount_long, @"exit", @"entryMarket");
							}
						}
						if (High[0] >= fix_trigger_price_long)
						{
							fix_stop_price_long = High[0] - current_stop * TrailingUnitsStop;
							fix_trigger_price_long = High[0];
							ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryMarket");
							Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
						}
					}
				}
				else if (my_entry_order.OrderState == OrderState.Filled/* && my_entry_market.OrderState != OrderState.Filled*/)
				{
					//					if (my_entry_order.OrderState == OrderState.Filled && my_entry_market.OrderState != OrderState.Filled)
					//					{
					//						if (my_entry_order.OrderType == OrderType.StopMarket && my_entry_order.OrderState == OrderState.Working && my_entry_order.OrderAction == OrderAction.SellShort)
					//						{
					//							Print (String.Format("{0} // {1} // {2} // {3}", my_entry_order.StopPrice, fix_stop_price_long,  my_entry_order.Quantity, Time[0]));
					//						}
					//						else
					//						{
					ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryOrder");
					//						}
					//					}				
					if (High[BarsSinceEntryExecution(0, @"entryOrder", 0)] < iSMA2[BarsSinceEntryExecution(0, @"entryOrder", 0)] + TicksToBO * TickSize)
					{
						bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
						{
							if (High[i] >= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
							{
								HighCrossEma50 = true; //And saves that value in this variable
								break;
							}
						}
						if (HighCrossEma50 == false)
						{
							if (Close[0] < iSMA1[0] - iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(fix_amount_long, @"exit", @"entryOrder");
							}
						}
						else
						{
							if (Close[0] < iSMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA1[0] - iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(fix_amount_long, @"exit", @"entryOrder");
							}
						}
					}
					else
					{
						if (Close[0] < iSMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA1[0] - iATR[0] * (MagicNumber / 100))
						{
							Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
							ExitLong(fix_amount_long, @"exit", @"entryOrder");
						}
					}
					if (High[0] >= fix_trigger_price_long)
					{
						fix_stop_price_long = High[0] - current_stop * TrailingUnitsStop;
						fix_trigger_price_long = High[0];
						ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryOrder");
						Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
					}
				}
				else if (my_entry_market.OrderState == OrderState.Filled/* && my_entry_order.OrderState != OrderState.Filled*/)
				{
					//					if (my_entry_market.OrderState == OrderState.Filled && my_entry_order.OrderState != OrderState.Filled)
					//					{
					if (my_entry_order.OrderType == OrderType.StopMarket && my_entry_order.OrderState == OrderState.Working && my_entry_order.OrderAction == OrderAction.SellShort)
					{
						//							Print (String.Format("{0} // {1} // {2} // {3}", my_entry_order.StopPrice, fix_stop_price_long,  my_entry_market.Quantity, Time[0]));
					}
					else
					{
						ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryMarket");
					}
					//					}				
					if (High[BarsSinceEntryExecution(0, @"entryMarket", 0)] < iSMA2[BarsSinceEntryExecution(0, @"entryMarket", 0)] + TicksToBO * TickSize)
					{
						bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
						{
							if (High[i] >= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
							{
								HighCrossEma50 = true; //And saves that value in this variable
								break;
							}
						}
						if (HighCrossEma50 == false)
						{
							if (Close[0] < iSMA1[0] - iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(fix_amount_long, @"exit", @"entryMarket");
							}
						}
						else
						{
							if (Close[0] < iSMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA1[0] - iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(fix_amount_long, @"exit", @"entryMarket");
							}
						}
					}
					else
					{
						if (Close[0] < iSMA2[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA1[0] - iATR[0] * (MagicNumber / 100))
						{
							Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
							ExitLong(fix_amount_long, @"exit", @"entryMarket");
						}
					}
					if (High[0] >= fix_trigger_price_long)
					{
						fix_stop_price_long = High[0] - current_stop * TrailingUnitsStop;
						fix_trigger_price_long = High[0];
						ExitLongStopMarket(fix_amount_long, fix_stop_price_long, @"exit", @"entryMarket");
						Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
					}
				}
			}
            #endregion

            #region Short
            ///While Short	
            if (Position.MarketPosition == MarketPosition.Short && !is_short) //if the postition is stil active and the initial stop and trailing stop trigger price levels were set then...
			{
				is_long = false;
				is_short = true;
				fix_amount_short = amount_short;
				fix_stop_price_short = stop_price_short;
				fix_trigger_price_short = trigger_price_short;
				Draw.FibonacciRetracements(this, "tag1" + CurrentBar, false, 0, Position.AveragePrice, 10, fix_stop_price_short);
			}
			if (Position.MarketPosition == MarketPosition.Short && is_short)
			{
				if (my_entry_order == null || my_entry_market == null)
				{
					if (my_entry_market == null && my_entry_order.OrderState == OrderState.Filled)
					{
						ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryOrder");
						//						}
						//					}
						if (Low[BarsSinceEntryExecution(0, @"entryOrder", 0)] > iSMA2[BarsSinceEntryExecution(0, @"entryOrder", 0)] - TicksToBO * TickSize)
						{
							bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
							{
								if (Low[i] <= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
									LowCrossEma50 = true; //And saves that value in this variable
									break;
								}
							}
							if (LowCrossEma50 == false)
							{
								if (Close[0] > iSMA1[0] + iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
									ExitShort(fix_amount_short, @"exit", @"entryOrder");
								}
							}
							else
							{
								if (Close[0] > iSMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA1[0] + iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
									ExitShort(fix_amount_short, @"exit", @"entryOrder");
								}
							}
						}
						else
						{
							if (Close[0] > iSMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA1[0] + iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(fix_amount_short, @"exit", @"entryOrder");
							}
						}
						if (Low[0] <= fix_trigger_price_short)
						{
							fix_stop_price_short = Low[0] + current_stop * TrailingUnitsStop;
							fix_trigger_price_short = Low[0];
							ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryOrder");
							Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Red);
						}
					}
					else if (my_entry_order == null && my_entry_market.OrderState == OrderState.Filled)
					{
						ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryMarket");
						//						}
						//					}
						if (Low[BarsSinceEntryExecution(0, @"entryMarket", 0)] > iSMA2[BarsSinceEntryExecution(0, @"entryMarket", 0)] - TicksToBO * TickSize)
						{
							bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
							{
								if (Low[i] <= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
								{
									LowCrossEma50 = true; //And saves that value in this variable
									break;
								}
							}
							if (LowCrossEma50 == false)
							{
								if (Close[0] > iSMA1[0] + iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
									ExitShort(fix_amount_short, @"exit", @"entryMarket");
								}
							}
							else
							{
								if (Close[0] > iSMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA1[0] + iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
									ExitShort(fix_amount_short, @"exit", @"entryMarket");
								}
							}
						}
						else
						{
							if (Close[0] > iSMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA1[0] + iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(fix_amount_short, @"exit", @"entryMarket");
							}
						}
						if (Low[0] <= fix_trigger_price_short)
						{
							fix_stop_price_short = Low[0] + current_stop * TrailingUnitsStop;
							fix_trigger_price_short = Low[0];
							ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryMarket");
							Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Red);
						}
					}
				}
				else if (my_entry_order.OrderState == OrderState.Filled/* && my_entry_market.OrderState != OrderState.Filled*/)
				{
					//					if (my_entry_order.OrderState == OrderState.Filled && my_entry_market.OrderState != OrderState.Filled)
					//					{
					//						if (my_entry_order.OrderType == OrderType.StopMarket && my_entry_order.OrderState == OrderState.Working && my_entry_order.OrderAction == OrderAction.Buy)
					//						{
					//							Print (String.Format("{0} // {1} // {2} // {3}", my_entry_order.StopPrice, fix_stop_price_long,  my_entry_order.Quantity, Time[0]));
					//						}
					//						else
					//						{
					ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryOrder");
					//						}
					//					}
					if (Low[BarsSinceEntryExecution(0, @"entryOrder", 0)] > iSMA2[BarsSinceEntryExecution(0, @"entryOrder", 0)] - TicksToBO * TickSize)
					{
						bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
						{
							if (Low[i] <= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
							{
								LowCrossEma50 = true; //And saves that value in this variable
								break;
							}
						}
						if (LowCrossEma50 == false)
						{
							if (Close[0] > iSMA1[0] + iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(fix_amount_short, @"exit", @"entryOrder");
							}
						}
						else
						{
							if (Close[0] > iSMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA1[0] + iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(fix_amount_short, @"exit", @"entryOrder");
							}
						}
					}
					else
					{
						if (Close[0] > iSMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA1[0] + iATR[0] * (MagicNumber / 100))
						{
							Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
							ExitShort(fix_amount_short, @"exit", @"entryOrder");
						}
					}
					if (Low[0] <= fix_trigger_price_short)
					{
						fix_stop_price_short = Low[0] + current_stop * TrailingUnitsStop;
						fix_trigger_price_short = Low[0];
						ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryOrder");
						Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Red);
					}
				}
				else if (my_entry_market.OrderState == OrderState.Filled/* && my_entry_order.OrderState != OrderState.Filled*/)
				{
					//					if (my_entry_market.OrderState == OrderState.Filled && my_entry_order.OrderState != OrderState.Filled)
					//					{
					if (my_entry_order.OrderType == OrderType.StopMarket && my_entry_order.OrderState == OrderState.Working && my_entry_order.OrderAction == OrderAction.Buy)
					{
						//							Print (String.Format("{0} // {1} // {2} // {3}", my_entry_order.StopPrice, fix_stop_price_short,  my_entry_market.Quantity, Time[0]));
					}
					else
					{
						ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryMarket");
					}
					//					}
					if (Low[BarsSinceEntryExecution(0, @"entryMarket", 0)] > iSMA2[BarsSinceEntryExecution(0, @"entryMarket", 0)] - TicksToBO * TickSize)
					{
						bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i = 0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
						{
							if (Low[i] <= iSMA2[i]) //To determine the highest value (max close value of the swinglow4)
							{
								LowCrossEma50 = true; //And saves that value in this variable
								break;
							}
						}
						if (LowCrossEma50 == false)
						{
							if (Close[0] > iSMA1[0] + iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(fix_amount_short, @"exit", @"entryMarket");
							}
						}
						else
						{
							if (Close[0] > iSMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA1[0] + iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(fix_amount_short, @"exit", @"entryMarket");
							}
						}
					}
					else
					{
						if (Close[0] > iSMA2[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA1[0] + iATR[0] * (MagicNumber / 100))
						{
							Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
							ExitShort(fix_amount_short, @"exit", @"entryMarket");
						}
					}
					if (Low[0] <= fix_trigger_price_short)
					{
						fix_stop_price_short = Low[0] + current_stop * TrailingUnitsStop;
						fix_trigger_price_short = Low[0];
						ExitShortStopMarket(fix_amount_short, fix_stop_price_short, @"exit", @"entryMarket");
						Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Red);
					}
				}
			}
            #endregion
            #endregion
        }

        #region Properties
        [NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "SMA1 (Max)", Order = 1, GroupName = "Parameters")]
		public int SMA1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "SMA2 (Mid)", Order = 2, GroupName = "Parameters")]
		public int SMA2
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "SMA3 (Min)", Order = 3, GroupName = "Parameters")]
		public int SMA3
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "ATR1", Order = 4, GroupName = "Parameters")]
		public int ATR1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Swing1", Order = 5, GroupName = "Parameters")]
		public int Swing1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Swing2", Order = 6, GroupName = "Parameters")]
		public int Swing2
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "TrailingUnitsStop", Order = 7, GroupName = "Parameters")]
		public double TrailingUnitsStop
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "UnitsTriggerForTrailing", Order = 8, GroupName = "Parameters")]
		public double UnitsTriggerForTrailing
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "RiskUnit", Order = 9, GroupName = "Parameters")]
		public int RiskUnit
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "IncipientTrandFactor", Order = 10, GroupName = "Parameters")]
		public double IncipientTrandFactor
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "ATRStopFactor", Order = 11, GroupName = "Parameters")]
		public double ATRStopFactor
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "TicksToBO", Order = 12, GroupName = "Parameters")]
		public double TicksToBO
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "SwingPenetration(%)", Order = 13, GroupName = "Parameters")]
		public double SwingPenetration
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "ClosnessFactor", Order = 14, GroupName = "Parameters")]
		public double ClosnessFactor
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "ClosnessToTrade", Order = 15, GroupName = "Parameters")]
		public double ClosnessToTrade
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "MagicNumber(Percent (%) of ATR)", Order = 16, GroupName = "Parameters")]
		public double MagicNumber
		{ get; set; }
        #endregion

        #region Functions
        ////	METHODS			

        ////	Swing Identification Method				
        public bool SwingIdentifiacation(ISeries<double> BarClose, ISeries<double> OppositeSwing, int ReferenceSwingBar, bool isPotentialSwing, string SwingType)
		{
			if (SwingType == "SwingHigh")
			{
				for (int i = 0; i <= ReferenceSwingBar; i++) // Walks every bar of the potential HL4
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
				for (int i = 0; i <= ReferenceSwingBar; i++) // Walks every bar of the potential HL4
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
		public Tuple<double, double, int> SwingLocation(ISeries<double> BarExtreme, ISeries<double> ReferenceSwing, int ReferenceSwingBar, string SwingType)
		{
			double SwingMidLevel = ReferenceSwing[0];
			double ExtremeLevel = BarExtreme[0]; //initializing the variable that is going to keep the max high value of the swing, with the array firts value for comparison purposes
			int ExtremeLevelBar = 0;
			if (SwingType == "SwingHigh")
			{
				double MinClose = Close[0]; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
				for (int i = 0; i <= ReferenceSwingBar; i++) // Walks every bar of the potential HL4
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
			}
			else if (SwingType == "SwingLow")
			{
				double MaxClose = Close[0]; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
				for (int i = 0; i <= ReferenceSwingBar; i++) // Walks every bar of the potential HL4
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
			}
			return new Tuple<double, double, int>(ExtremeLevel, SwingMidLevel, ExtremeLevelBar);
		}

		////	Buy Process	Method		
		public bool Buy(string OrderType, ISeries<double> BOLevel)
		{
			if (OrderType == "MarketOrder")
			{
				amount_long = Convert.ToInt32((RiskUnit / ((current_stop / TickSize) * Instrument.MasterInstrument.PointValue * TickSize))); //calculates trade amount
				EnterLong(amount_long, @"entryMarket"); // Long Stop order activation							
				stop_price_long = Close[0] - current_stop; //calculates the stop price level
				trigger_price_long = Close[0] + current_stop * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger	
				return true;
			}
			else if (OrderType == "PendingOrder")
			{
				amount_long = Convert.ToInt32((RiskUnit / ((current_stop / TickSize) * Instrument.MasterInstrument.PointValue * TickSize)));
				EnterLongStopMarket(amount_long, BOLevel[0] + TicksToBO * TickSize, @"entryOrder");
				stop_price_long = (BOLevel[0] + TicksToBO * TickSize) - current_stop; //calculates the stop price level
				trigger_price_long = (BOLevel[0] + TicksToBO * TickSize) + current_stop * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger							
				return false;
			}
			else
			{
				return false;
			}
		}

		////	Sell Process Method	 			
		public bool Sell(string OrderType, ISeries<double> BOLevel)
		{
			if (OrderType == "MarketOrder")
			{
				amount_short = Convert.ToInt32((RiskUnit / ((current_stop / TickSize) * Instrument.MasterInstrument.PointValue * TickSize))); //calculates trade amount
				EnterShort(amount_short, @"entryMarket"); // Long Stop order activation							
				stop_price_short = Close[0] + current_stop; //calculates the stop price level
				trigger_price_short = Close[0] - current_stop * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger	
				return true;
			}
			else if (OrderType == "PendingOrder")
			{
				amount_short = Convert.ToInt32((RiskUnit / ((current_stop / TickSize) * Instrument.MasterInstrument.PointValue * TickSize)));
				EnterShortStopMarket(amount_short, BOLevel[0] - TicksToBO * TickSize, @"entryOrder");
				stop_price_short = (BOLevel[0] - TicksToBO * TickSize) + current_stop; //calculates the stop price level
				trigger_price_short = (BOLevel[0] - TicksToBO * TickSize) - current_stop * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger					
				Print(String.Format("{0} // {1} // {2} // {3}", amount_short, BOLevel[0], stop_price_short, T
				return false;
			}
			else
			{
				return false;
			}
		}

		////	Swing4 operation Method			
		///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4	
		public Tuple<bool, bool, bool> Swing4(ISeries<double> ReferenceSMA, string Type)
		{
			////		SWING 4 HIGH			
			if (Type == "High")
			{
				bool isSwingHigh4 = SwingIdentifiacation(Close, iSwing4.SwingLow, iSwing4.SwingHighBar(0, 1, CurrentBar), true, "SwingHigh");
				bool isActiveLongPosition = false;
				if (isSwingHigh4) //if the HL4 is confirmated after the Last two validations then...
				{
					Tuple<double, double, int> ReturnedValues = SwingLocation(High, iSwing4.SwingHigh, iSwing4.SwingHighBar(0, 1, CurrentBar), "SwingHigh");
					max_high_swingHigh4 = ReturnedValues.Item1;
					double SwingHigh4MidLevel = ReturnedValues.Item2;
					int ExtremeLevelBar = ReturnedValues.Item3;
					if (SwingHigh4MidLevel >= ReferenceSMA[0] /*&& max_high_swingHigh4 < iSwing4.SwingHigh[0] + TicksToBO * TickSize*/)
					{
						if (iSwing14.SwingHigh[0] > iSwing4.SwingHigh[0] && iSwing14.SwingHigh[0] <= iSwing4.SwingHigh[0] + iATR[0] * ClosnessFactor)
						{
							if ((iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA1[0] <= current_stop || (iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA2[0] <= current_stop)
							{
								if (my_entry_order != null && my_exit_order != null)
								{
									if ((my_entry_order.OrderType == OrderType.StopMarket || my_exit_order.OrderType == OrderType.StopMarket) && (my_entry_order.OrderState == OrderState.Working || my_entry_order.OrderState == OrderState.Filled || my_exit_order.OrderState == OrderState.Working) && (my_entry_order.OrderAction == OrderAction.SellShort || my_exit_order.OrderAction == OrderAction.SellShort))
									{
										Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing14.SwingHigh, iSwing4.SwingHigh, ReferenceSMA, ExtremeLevelBar, is_BO_up_swing4, max_high_swingHigh4, "Up");
										is_BO_up_swing4 = ReturnedValues1.Item1;
										isActiveLongPosition = ReturnedValues1.Item2;
									}
									else
									{
										if (max_high_swingHigh4 < iSwing14.SwingHigh[0] + TicksToBO * TickSize)
										{
											Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing14.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Lime); //Draw a mark to know where all conditions have been met
											isActiveLongPosition = Buy("PendingOrder", iSwing14.SwingHigh);

										}
										else
										{
											is_BO_up_swing4 = true;
										}
									}
								}
								else
								{
									if (max_high_swingHigh4 < iSwing14.SwingHigh[0] + TicksToBO * TickSize)
									{
										Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing14.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Lime); //Draw a mark to know where all conditions have been met
										isActiveLongPosition = Buy("PendingOrder", iSwing14.SwingHigh);

									}
									else
									{
										is_BO_up_swing4 = true;
									}
								}
							}
						}
						else
						{
							if (my_entry_order != null && my_exit_order != null)
							{
								if ((my_entry_order.OrderType == OrderType.StopMarket || my_exit_order.OrderType == OrderType.StopMarket) && (my_entry_order.OrderState == OrderState.Working || my_entry_order.OrderState == OrderState.Filled || my_exit_order.OrderState == OrderState.Working) && (my_entry_order.OrderAction == OrderAction.SellShort || my_exit_order.OrderAction == OrderAction.SellShort))
								{
									Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing4.SwingHigh, iSwing4.SwingHigh, ReferenceSMA, ExtremeLevelBar, is_BO_up_swing4, max_high_swingHigh4, "Up");
									is_BO_up_swing4 = ReturnedValues1.Item1;
									isActiveLongPosition = ReturnedValues1.Item2;
								}
								else
								{
									if (max_high_swingHigh4 < iSwing4.SwingHigh[0] + TicksToBO * TickSize)
									{
										Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing4.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Lime); //Draw a mark to know where all conditions have been met
										isActiveLongPosition = Buy("PendingOrder", iSwing4.SwingHigh);

									}
									else
									{
										is_BO_up_swing4 = true;
									}
								}
							}
							else
							{
								if (max_high_swingHigh4 < iSwing4.SwingHigh[0] + TicksToBO * TickSize)
								{
									Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing4.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Lime); //Draw a mark to know where all conditions have been met
									isActiveLongPosition = Buy("PendingOrder", iSwing4.SwingHigh);

								}
								else
								{
									is_BO_up_swing4 = true;
								}
							}
						}
					}
					else if (iSwing4.SwingHigh[0] >= ReferenceSMA[0])
					{
						if (iSwing14.SwingHigh[0] > iSwing4.SwingHigh[0] && iSwing14.SwingHigh[0] <= iSwing4.SwingHigh[0] + iATR[0] * ClosnessFactor)
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing14.SwingHigh, ReferenceSMA, max_high_swingHigh4, ExtremeLevelBar, 0, "Up", "Swing4", is_BO_up_swing4);
							is_BO_up_swing4 = ReturnedValues1.Item1;
							isActiveLongPosition = ReturnedValues1.Item2;
						}
						else
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing4(iSwing4.SwingHigh, ReferenceSMA, ExtremeLevelBar, 0, "Up");
							is_BO_up_swing4 = ReturnedValues1.Item1;
							isActiveLongPosition = ReturnedValues1.Item2;
						}
					}
					else
					{
						if (iSwing14.SwingHigh[0] > iSwing4.SwingHigh[0] && iSwing14.SwingHigh[0] <= iSwing4.SwingHigh[0] + iATR[0] * ClosnessFactor)
						{
							if (iSwing14.SwingHigh[0] < ReferenceSMA[0])
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(ReferenceSMA, ReferenceSMA, max_high_swingHigh4, ExtremeLevelBar, ExtremeLevelBar, "Up", "Swing4", is_BO_up_swing4);
								is_BO_up_swing4 = ReturnedValues1.Item1;
								isActiveLongPosition = ReturnedValues1.Item2;
							}
							else
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing14.SwingHigh, ReferenceSMA, max_high_swingHigh4, ExtremeLevelBar, 0, "Up", "Swing4", is_BO_up_swing4);
								is_BO_up_swing4 = ReturnedValues1.Item1;
								isActiveLongPosition = ReturnedValues1.Item2;
							}
						}
						else
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing4(ReferenceSMA, ReferenceSMA, ExtremeLevelBar, ExtremeLevelBar, "Up");
							is_BO_up_swing4 = ReturnedValues1.Item1;
							isActiveLongPosition = ReturnedValues1.Item2;
						}
					}
				}
				if (is_BO_up_swing4)
				{
					isSwingHigh4 = false;
				}
				return new Tuple<bool, bool, bool>(isSwingHigh4, isActiveLongPosition, is_BO_up_swing4);
			}

			////		SWING 4 LOW
			else if (Type == "Low")
			{
				bool isSwingLow4 = SwingIdentifiacation(Close, iSwing4.SwingHigh, iSwing4.SwingLowBar(0, 1, CurrentBar), true, "SwingLow");
				bool isActiveShortPosition = false;
				if (isSwingLow4)
				{
					Tuple<double, double, int> ReturnedValues = SwingLocation(Low, iSwing4.SwingLow, iSwing4.SwingLowBar(0, 1, CurrentBar), "SwingLow");
					min_low_swingLow4 = ReturnedValues.Item1;
					double SwingLow4MidLevel = ReturnedValues.Item2;
					int ExtremeLevelBar = ReturnedValues.Item3;
					if (SwingLow4MidLevel <= ReferenceSMA[0] /*&& min_low_swingLow4 > iSwing4.SwingLow[0] - TicksToBO * TickSize*/)
					{
						if (iSwing14.SwingLow[0] < iSwing4.SwingLow[0] && iSwing14.SwingLow[0] >= iSwing4.SwingLow[0] - iATR[0] * ClosnessFactor)
						{
							if (iSMA1[0] - iSwing14.SwingLow[0] - TicksToBO * TickSize <= current_stop || iSMA2[0] - iSwing14.SwingLow[0] - TicksToBO * TickSize <= current_stop)
							{
								if (my_entry_order != null && my_exit_order != null)
								{
									if ((my_entry_order.OrderType == OrderType.StopMarket || my_exit_order.OrderType == OrderType.StopMarket) && (my_entry_order.OrderState == OrderState.Working || my_entry_order.OrderState == OrderState.Filled || my_exit_order.OrderState == OrderState.Working) && (my_entry_order.OrderAction == OrderAction.Buy || my_exit_order.OrderAction == OrderAction.Buy))
									{
										Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing14.SwingLow, iSwing4.SwingLow, ReferenceSMA, ExtremeLevelBar, is_BO_down_swing4, min_low_swingLow4, "Down");
										is_BO_down_swing4 = ReturnedValues1.Item1;
										isActiveShortPosition = ReturnedValues1.Item2;
									}
									else
									{
										if (min_low_swingLow4 > iSwing14.SwingLow[0] - TicksToBO * TickSize)
										{
											Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing14.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Red);
											isActiveShortPosition = Sell("PendingOrder", iSwing14.SwingLow);
										}
										else
										{
											is_BO_down_swing4 = true;
										}
									}
								}
								else
								{
									if (min_low_swingLow4 > iSwing14.SwingLow[0] - TicksToBO * TickSize)
									{
										Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing14.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Red);
										isActiveShortPosition = Sell("PendingOrder", iSwing14.SwingLow);
									}
									else
									{
										is_BO_down_swing4 = true;
									}
								}
							}
						}
						else
						{
							if (my_entry_order != null && my_exit_order != null)
							{
								if ((my_entry_order.OrderType == OrderType.StopMarket || my_exit_order.OrderType == OrderType.StopMarket) && (my_entry_order.OrderState == OrderState.Working || my_entry_order.OrderState == OrderState.Filled || my_exit_order.OrderState == OrderState.Working) && (my_entry_order.OrderAction == OrderAction.Buy || my_exit_order.OrderAction == OrderAction.Buy))
								{
									Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing4.SwingLow, iSwing4.SwingLow, ReferenceSMA, ExtremeLevelBar, is_BO_down_swing4, min_low_swingLow4, "Down");
									is_BO_down_swing4 = ReturnedValues1.Item1;
									isActiveShortPosition = ReturnedValues1.Item2;
								}
								else
								{
									if (min_low_swingLow4 > iSwing4.SwingLow[0] - TicksToBO * TickSize)
									{
										Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing4.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Red);
										isActiveShortPosition = Sell("PendingOrder", iSwing4.SwingLow);
									}
									else
									{
										is_BO_down_swing4 = true;
									}
								}
							}
							else
							{
								if (min_low_swingLow4 > iSwing4.SwingLow[0] - TicksToBO * TickSize)
								{
									Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing4.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Red);
									isActiveShortPosition = Sell("PendingOrder", iSwing4.SwingLow);
								}
								else
								{
									is_BO_down_swing4 = true;
								}
							}
						}
					}
					else if (iSwing4.SwingLow[0] <= ReferenceSMA[0])
					{
						if (iSwing14.SwingLow[0] < iSwing4.SwingLow[0] && iSwing14.SwingLow[0] >= iSwing4.SwingLow[0] - iATR[0] * ClosnessFactor)
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing14.SwingLow, ReferenceSMA, min_low_swingLow4, ExtremeLevelBar, 0, "Down", "Swing4", is_BO_down_swing4);
							is_BO_down_swing4 = ReturnedValues1.Item1;
							isActiveShortPosition = ReturnedValues1.Item2;
						}
						else
						{
							//							Print (String.Format("{0} // {1} // {2} // {3}", is_BO_down_swing4, iSwing14.SwingLow[0], iSwing4.SwingLow[0], Time[0]));
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing4(iSwing4.SwingLow, ReferenceSMA, ExtremeLevelBar, 0, "Down");
							is_BO_down_swing4 = ReturnedValues1.Item1;
							isActiveShortPosition = ReturnedValues1.Item2;
						}
					}
					else
					{
						if (iSwing14.SwingLow[0] < iSwing4.SwingLow[0] && iSwing14.SwingLow[0] >= iSwing4.SwingLow[0] - iATR[0] * ClosnessFactor)
						{
							if (iSwing14.SwingLow[0] > ReferenceSMA[0])
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(ReferenceSMA, ReferenceSMA, min_low_swingLow4, ExtremeLevelBar, ExtremeLevelBar, "Down", "Swing4", is_BO_down_swing4);
								is_BO_down_swing4 = ReturnedValues1.Item1;
								isActiveShortPosition = ReturnedValues1.Item2;
							}
							else
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing14.SwingLow, ReferenceSMA, min_low_swingLow4, ExtremeLevelBar, 0, "Down", "Swing4", is_BO_down_swing4);
								is_BO_down_swing4 = ReturnedValues1.Item1;
								isActiveShortPosition = ReturnedValues1.Item2;
							}
						}
						else
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing4(ReferenceSMA, ReferenceSMA, ExtremeLevelBar, ExtremeLevelBar, "Down");
							is_BO_down_swing4 = ReturnedValues1.Item1;
							isActiveShortPosition = ReturnedValues1.Item2;
						}
					}
				}
				if (is_BO_down_swing4)
				{
					isSwingLow4 = false;
				}
				return new Tuple<bool, bool, bool>(isSwingLow4, isActiveShortPosition, is_BO_down_swing4);
			}
			return new Tuple<bool, bool, bool>(false, false, false);
		}

		////	Swing14 operation Method				
		public Tuple<bool, bool, bool> Swing14(ISeries<double> ReferenceSMA, string Type)
		{
			////		SWING 14 HIGH
			if (Type == "High")
			{
				bool isSwingHigh14 = SwingIdentifiacation(Close, iSwing14.SwingLow, iSwing14.SwingHighBar(0, 1, CurrentBar), true, "SwingHigh");
				bool isActiveLongPosition = false;
				if (isSwingHigh14)
				{
					Tuple<double, double, int> ReturnedValues = SwingLocation(High, iSwing14.SwingHigh, iSwing14.SwingHighBar(0, 1, CurrentBar), "SwingHigh");
					max_high_swingHigh14 = ReturnedValues.Item1;
					double SwingHigh14MidLevel = ReturnedValues.Item2;
					int ExtremeLevelBar = ReturnedValues.Item3;
					if (SwingHigh14MidLevel >= ReferenceSMA[0]  /*&& max_high_swingHigh14 < iSwing14.SwingHigh[0] + TicksToBO * TickSize*/)
					{
						if (my_entry_order != null && my_exit_order != null)
						{
							if ((my_entry_order.OrderType == OrderType.StopMarket || my_exit_order.OrderType == OrderType.StopMarket) && (my_entry_order.OrderState == OrderState.Working || my_entry_order.OrderState == OrderState.Filled || my_exit_order.OrderState == OrderState.Working) && (my_entry_order.OrderAction == OrderAction.SellShort || my_exit_order.OrderAction == OrderAction.SellShort))
							{
								Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing14.SwingHigh, iSwing14.SwingHigh, ReferenceSMA, ExtremeLevelBar, is_BO_up_swing14, max_high_swingHigh14, "Up");
								is_BO_up_swing14 = ReturnedValues1.Item1;
								isActiveLongPosition = ReturnedValues1.Item2;
							}
							else
							{

								if (max_high_swingHigh14 < iSwing14.SwingHigh[0] + TicksToBO * TickSize)
								{
									Draw.Dot(this, @"GreenDot" + CurrentBar, true, 0, iSwing14.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Green);
									isActiveLongPosition = Buy("PendingOrder", iSwing14.SwingHigh);
								}
								else
								{
									is_BO_up_swing14 = true;
								}
							}
						}
						else
						{
							if (max_high_swingHigh14 < iSwing14.SwingHigh[0] + TicksToBO * TickSize)
							{
								Draw.Dot(this, @"GreenDot" + CurrentBar, true, 0, iSwing14.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Green);
								isActiveLongPosition = Buy("PendingOrder", iSwing14.SwingHigh);
							}
							else
							{
								is_BO_up_swing14 = true;
							}
						}
					}
					else if (iSwing14.SwingHigh[0] >= ReferenceSMA[0])
					{
						Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing14.SwingHigh, ReferenceSMA, max_high_swingHigh14, ExtremeLevelBar, 0, "Up", "Swing14", is_BO_up_swing14);
						is_BO_up_swing14 = ReturnedValues1.Item1;
						isActiveLongPosition = ReturnedValues1.Item2;
					}
					else
					{
						Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(ReferenceSMA, ReferenceSMA, max_high_swingHigh14, ExtremeLevelBar, ExtremeLevelBar, "Up", "Swing14", is_BO_up_swing14);
						is_BO_up_swing14 = ReturnedValues1.Item1;
						isActiveLongPosition = ReturnedValues1.Item2;
					}
				}
				if (is_BO_up_swing14)
				{
					isSwingHigh14 = false;
				}
				return new Tuple<bool, bool, bool>(isSwingHigh14, isActiveLongPosition, is_BO_up_swing14);
			}

			////		SWING 14 LOW
			else if (Type == "Low")
			{
				bool isSwingLow14 = SwingIdentifiacation(Close, iSwing14.SwingHigh, iSwing14.SwingLowBar(0, 1, CurrentBar), true, "SwingLow");
				bool isActiveShortPosition = false;
				if (isSwingLow14)
				{
					Tuple<double, double, int> ReturnedValues = SwingLocation(Low, iSwing14.SwingLow, iSwing14.SwingLowBar(0, 1, CurrentBar), "SwingLow");
					min_low_swingLow14 = ReturnedValues.Item1;
					double SwingLow14MidLevel = ReturnedValues.Item2;
					int ExtremeLevelBar = ReturnedValues.Item3;
					if (SwingLow14MidLevel <= ReferenceSMA[0] /*&& min_low_swingLow14 > iSwing14.SwingLow[0] - TicksToBO * TickSize*/)
					{
						if (my_entry_order != null && my_exit_order != null)
						{
							if ((my_entry_order.OrderType == OrderType.StopMarket || my_exit_order.OrderType == OrderType.StopMarket) && (my_entry_order.OrderState == OrderState.Working || my_entry_order.OrderState == OrderState.Filled || my_exit_order.OrderState == OrderState.Working) && (my_entry_order.OrderAction == OrderAction.Buy || my_exit_order.OrderAction == OrderAction.Buy))
							{
								Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing14.SwingLow, iSwing14.SwingLow, ReferenceSMA, ExtremeLevelBar, is_BO_down_swing14, min_low_swingLow14, "Down");
								is_BO_down_swing14 = ReturnedValues1.Item1;
								isActiveShortPosition = ReturnedValues1.Item2;
							}
							else
							{
								if (min_low_swingLow14 > iSwing14.SwingLow[0] - TicksToBO * TickSize)
								{
									Draw.Dot(this, @"MaroonDot" + CurrentBar, true, 0, iSwing14.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Maroon);
									isActiveShortPosition = Sell("PendingOrder", iSwing14.SwingLow);
								}
								else
								{
									is_BO_down_swing14 = true;
								}
							}
						}
						else
						{
							if (min_low_swingLow14 > iSwing14.SwingLow[0] - TicksToBO * TickSize)
							{
								Draw.Dot(this, @"MaroonDot" + CurrentBar, true, 0, iSwing14.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Maroon);
								isActiveShortPosition = Sell("PendingOrder", iSwing14.SwingLow);
							}
							else
							{
								is_BO_down_swing14 = true;
							}
						}
					}
					else if (iSwing14.SwingLow[0] <= ReferenceSMA[0])
					{
						Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing14.SwingLow, ReferenceSMA, min_low_swingLow14, ExtremeLevelBar, 0, "Down", "Swing14", is_BO_down_swing14);
						is_BO_down_swing14 = ReturnedValues1.Item1;
						isActiveShortPosition = ReturnedValues1.Item2;
					}
					else
					{
						Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(ReferenceSMA, ReferenceSMA, min_low_swingLow14, ExtremeLevelBar, ExtremeLevelBar, "Down", "Swing14", is_BO_down_swing14);
						is_BO_down_swing14 = ReturnedValues1.Item1;
						isActiveShortPosition = ReturnedValues1.Item2;
					}
				}
				if (is_BO_down_swing14)
				{
					isSwingLow14 = false;
				}
				return new Tuple<bool, bool, bool>(isSwingLow14, isActiveShortPosition, is_BO_down_swing14);
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
								if (Close[0] - ReferenceSMA[0] <= current_stop)
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
					if (ExtremeLevel <= iSwing14.SwingHigh[0])
					{
						isBO = false;
					}
					else
					{
						if (Swing == "Swing4")
						{
							if (Close[0] >= last_max_high_swingHigh4 + TicksToBO * TickSize)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, last_max_high_swingHigh4 + 3 * TicksToBO * TickSize, Brushes.Cyan);
								if (Close[0] - ReferenceSMA[0] <= current_stop)
								{
									isActiveLongPosition = Buy("MarketOrder", Close);
								}
							}
						}
						else if (Swing == "Swing14")
						{
							if (Close[0] >= last_max_high_swingHigh14 + TicksToBO * TickSize && ReferenceBar == 0)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, last_max_high_swingHigh14 + 3 * TicksToBO * TickSize, Brushes.Cyan);
								if (Close[0] - ReferenceSMA[0] <= current_stop)
								{
									isActiveLongPosition = Buy("MarketOrder", Close);
								}
							}
						}
					}
				}
				return new Tuple<bool, bool>(isBO, isActiveLongPosition);
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
								if (ReferenceSMA[0] - Close[0] <= current_stop)
								{
									isActiveShortPosition = Sell("MarketOrder", Close);
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
					if (ExtremeLevel >= iSwing14.SwingLow[0])
					{
						isBO = false;
					}
					else
					{
						if (Swing == "Swing4")
						{
							if (Close[0] <= last_min_low_swingLow4 - TicksToBO * TickSize)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, last_min_low_swingLow4 - 3 * TicksToBO * TickSize, Brushes.Cyan);
								if (ReferenceSMA[0] - Close[0] <= current_stop)
								{
									isActiveShortPosition = Sell("MarketOrder", Close);
								}
							}
						}
						else if (Swing == "Swing14")
						{
							if (Close[0] <= last_min_low_swingLow14 - TicksToBO * TickSize && ReferenceBar == 0)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, last_min_low_swingLow14 - 3 * TicksToBO * TickSize, Brushes.Cyan);
								if (ReferenceSMA[0] - Close[0] <= current_stop)
								{
									isActiveShortPosition = Sell("MarketOrder", Close);
								}
							}
						}
					}
				}
				return new Tuple<bool, bool>(isBO, isActiveShortPosition);
			}
			return new Tuple<bool, bool>(isBO, isActiveLongPosition);
		}

		////	BO Swing4 proof when market order oportunity				
		public Tuple<bool, bool> BOProofSwing4(ISeries<double> ReferenceBOLevel, ISeries<double> ReferenceSMA, int ReferenceBar, int ReferenceSMABOBar, string Type)
		{
			////		UP
			bool isActiveLongPosition = false;
			bool isActiveShortPosition = false;
			if (Type == "Up")
			{
				if (!is_BO_up_swing4)
				{
					if (High[0] >= ReferenceBOLevel[0] + TicksToBO * TickSize)
					{
						if (ReferenceBar == 0)
						{
							Draw.Square(this, @"LimeSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
							is_BO_up_swing4 = true;
							if (Close[0] >= ReferenceBOLevel[0] + TicksToBO * TickSize)
							{
								if (iSwing14.SwingHigh[0] + TicksToBO * TickSize < Close[0])
								{
									if (Close[0] - ReferenceSMA[0] <= current_stop)
									{
										isActiveLongPosition = Buy("MarketOrder", Close);
									}
								}
								else
								{
									if (Close[0] - ReferenceSMA[0] <= current_stop && iSwing14.SwingHigh[0] - Close[0] > iATR[0] * ClosnessFactor)
									{
										isActiveLongPosition = Buy("MarketOrder", Close);
									}
								}
							}
						}
						else
						{
							is_BO_up_swing4 = true;
						}
					}
					else
					{
						if (max_high_swingHigh4 <= ReferenceBOLevel[ReferenceSMABOBar])
						{
							if (max_high_swingHigh4 >= ReferenceBOLevel[ReferenceSMABOBar] + TicksToBO * TickSize)
							{
								is_BO_up_swing4 = true;
							}
						}
						else
						{
							if (max_high_swingHigh4 >= ReferenceBOLevel[0] + TicksToBO * TickSize)
							{
								is_BO_up_swing4 = true;
							}
						}
					}
					if (!is_BO_up_swing4)
					{
						Draw.Square(this, @"LimeSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
					}
				}
				else
				{
					if (max_high_swingHigh4 <= iSwing4.SwingHigh[0])
					{
						is_BO_up_swing4 = false;
					}
					else if (Close[0] >= last_max_high_swingHigh4 + TicksToBO * TickSize && ReferenceBar == 0)
					{
						Draw.Dot(this, @"CyanSquare" + CurrentBar, true, 0, last_max_high_swingHigh4 + 3 * TicksToBO * TickSize, Brushes.Cyan);
						if (iSwing14.SwingHigh[0] + TicksToBO * TickSize < Close[0])
						{
							if (Close[0] - ReferenceSMA[0] <= current_stop)
							{
								isActiveLongPosition = Buy("MarketOrder", Close);
							}
						}
						else
						{
							if (Close[0] - ReferenceSMA[0] <= current_stop && iSwing14.SwingHigh[0] - Close[0] > iATR[0] * ClosnessFactor)
							{
								isActiveLongPosition = Buy("MarketOrder", Close);
							}
						}
					}
				}
				return new Tuple<bool, bool>(is_BO_up_swing4, isActiveLongPosition);
			}

			////		DOWN
			else if (Type == "Down")
			{
				if (!is_BO_down_swing4)
				{
					if (Low[0] <= ReferenceBOLevel[0] - TicksToBO * TickSize)
					{
						if (ReferenceBar == 0)
						{
							Draw.Square(this, @"RedSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] - 3 * TicksToBO * TickSize, Brushes.Red);
							is_BO_down_swing4 = true;
							if (Close[0] <= ReferenceBOLevel[0] - TicksToBO * TickSize)
							{
								if (iSwing14.SwingLow[0] > Close[0])
								{
									if (ReferenceSMA[0] - Close[0] <= current_stop)
									{
										isActiveShortPosition = Sell("MarketOrder", Close);
									}
								}
								else
								{
									if (ReferenceSMA[0] - Close[0] <= current_stop && Close[0] - iSwing14.SwingLow[0] > iATR[0] * ClosnessFactor)
									{
										isActiveShortPosition = Sell("MarketOrder", Close);
									}
								}
							}
						}
						else
						{
							is_BO_down_swing4 = true;
						}
					}
					else
					{
						if (min_low_swingLow4 >= ReferenceBOLevel[ReferenceSMABOBar])
						{
							if (min_low_swingLow4 <= ReferenceBOLevel[ReferenceSMABOBar] - TicksToBO * TickSize)
							{
								is_BO_down_swing4 = true;
							}
						}
						else
						{
							if (min_low_swingLow4 <= ReferenceBOLevel[0] - TicksToBO * TickSize)
							{
								is_BO_down_swing4 = true;
							}
						}
					}
					if (!is_BO_down_swing4)
					{
						Draw.Square(this, @"RedSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] - 3 * TicksToBO * TickSize, Brushes.Red);
					}
				}
				else
				{
					if (min_low_swingLow4 >= iSwing4.SwingLow[0])
					{
						is_BO_down_swing4 = false;
					}
					else if (Close[0] <= last_min_low_swingLow4 - TicksToBO * TickSize && ReferenceBar == 0)
					{
						Draw.Dot(this, @"CyanSquare" + CurrentBar, true, 0, last_min_low_swingLow4 - 3 * TicksToBO * TickSize, Brushes.Indigo);
						if (iSwing14.SwingLow[0] > Close[0])
						{
							if (ReferenceSMA[0] - Close[0] <= current_stop)
							{
								isActiveShortPosition = Sell("MarketOrder", Close);
							}
						}
						else
						{
							if (ReferenceSMA[0] - Close[0] <= current_stop && Close[0] - iSwing14.SwingLow[0] > iATR[0] * ClosnessFactor)
							{
								isActiveShortPosition = Sell("MarketOrder", Close);
							}
						}
					}
				}
				return new Tuple<bool, bool>(is_BO_down_swing4, isActiveShortPosition);
			}
			return new Tuple<bool, bool>(is_BO_up_swing4, isActiveLongPosition);
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
							if (Close[0] - ReferenceSMA[0] <= current_stop)
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
				return new Tuple<bool, bool>(isBO, isActiveLongPosition);
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
							if (ReferenceSMA[0] - Close[0] <= current_stop)
							{
								isActiveShortPosition = Sell("MarketOrder", Close);
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
				return new Tuple<bool, bool>(isBO, isActiveShortPosition);
			}
			return new Tuple<bool, bool>(isBO, isActiveLongPosition);
		}
        #endregion
    }
}
