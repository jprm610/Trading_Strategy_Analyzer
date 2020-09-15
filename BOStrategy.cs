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
	public class BOStrategyV93 : Strategy
	{
		#region Variables
		private SMA iSMA200, iSMA50, iSMA20;
		private ATR iATR;
		private Swing iSwing4, iSwing14;
		private ArrayList LastSwingHigh14Cache, LastSwingLow14Cache;
		private int CrossAboveBar, CrossBelowBar, AmountLong, FixAmountLong, AmountShort, FixAmountShort;
		private double ATRCrossingTime, SMAdif, StopPriceLong, TriggerPriceLong, StopPriceShort, TriggerPriceShort, SwingHigh14max, SwingLow14min, MaxCloseSwingLow4, RangeSizeSwingLow4, MaxCloseSwingLow14;
		private double LastMaxHighSwingHigh4, LastMaxHighSwingHigh14, LastMinLowSwingLow4, LastMinLowSwingLow14;
		private double RangeSizeSwingLow14, MinCloseSwingHigh4, RangeSizeSwingHigh4, MinCloseSwingHigh14, RangeSizeSwingHigh14, SwingHigh14maxReentry, SwingLow14minReentry, MaxHighSwingHigh4, MaxHighSwingHigh14, MinLowSwingLow4, MinLowSwingLow14;
		private double CurrentSwingHigh14, CurrentSwingLow14, CurrentSwingHigh4, CurrentSwingLow4, CurrentATR, CurrentStop, CurrentOpen, CurrentClose, CurrentHigh, CurrentLow, FixStopPriceLong, FixTriggerPriceLong, FixStopPriceShort, FixTriggerPriceShort;
		private bool isIncipientUpTrend, isIncipientDownTrend, IsUpWard = true, IsDownWard = true, GrayEllipseLong, GrayEllipseShort, isReentryLong, isReentryShort, isLong, isShort, isBO, isBOUpSwing4, isBOUpSwing14, isBODownSwing4, isBODownSwing14;			
		private bool CrossBelow20_50, CrossAbove20_50;
		private Account myAccount;
		private Order myEntryOrder = null, myEntryMarket = null, myExitOrder = null;
        #endregion

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Strategy that Operates Breakouts";
				Name										= "BOStrategyV93";
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
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				SMA1						= 200;
				SMA2						= 50;
				SMA3						= 20;
				ATR1						= 100;
				Swing1						= 4;
				Swing2						= 14;
				UnitsTriggerForTrailing		= 1;				
				TrailerUnitsStop			= 2;
				RiskUnit                    = 420;	
				IncipientTrandFactor		= 2;
				ATRStopFactor				= 3;
				TicksToBO					= 10;
				SwingPenetration			= 50;
				ClosnessFactor				= 2;
				ClosnessToTrade				= 1;
				MagicNumber					= 50;
			}
			else if (State == State.Configure)
			{
				AddDataSeries("GBPUSD", Data.BarsPeriodType.Minute, 60, Data.MarketDataType.Last);
			}
			else if (State == State.DataLoaded)
			{				
				iSMA200	= SMA(Close, SMA1);
				iSMA50 = SMA(Close, SMA2);
				iSMA20 = SMA(Close, SMA3);
				iATR = ATR(Close, ATR1);
				iSwing4	= Swing(Close, Swing1);
				iSwing14 = Swing(Close, Swing2);
				iSMA200.Plots[0].Brush = Brushes.Red;
				iSMA50.Plots[0].Brush = Brushes.Gold;
				iSMA20.Plots[0].Brush = Brushes.Lime;
				iATR.Plots[0].Brush = Brushes.White;
				iSwing4.Plots[0].Brush = Brushes.Fuchsia;
				iSwing4.Plots[1].Brush = Brushes.Gold;
				iSwing14.Plots[0].Brush = Brushes.Silver;
				iSwing14.Plots[1].Brush = Brushes.Silver;
				AddChartIndicator(iSMA200);
				AddChartIndicator(iSMA50);
				AddChartIndicator(iSMA20);
				AddChartIndicator(iATR);
				AddChartIndicator(iSwing4);
				AddChartIndicator(iSwing14);				
				LastSwingHigh14Cache = new ArrayList();
				LastSwingLow14Cache	= new ArrayList();
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
		
////	OnBarUpdate Method
		protected override void OnBarUpdate()
		{	
//			if (myEntryOrder != null)
//			{
//				Print (String.Format("{0} // {1} // {2}", myEntryOrder, myEntryOrder.Quantity, Time[0]));
//			}
//			if (myEntryMarket != null)
//			{
//				Print (String.Format("{0} // {1} // {2}", myEntryMarket, myEntryMarket.Quantity, Time[0]));
//			}
//			if (myExitOrder != null)
//			{
//				Print (String.Format("{0} // {1} // {2}", myExitOrder, myExitOrder.Quantity, Time[0]));
//			}
			
////		Properly stop setting confirmation			
			if (TrailerUnitsStop - UnitsTriggerForTrailing > 1)
			{
				Print("You should correct the stop input data");
				return;
			}		
			
////		Bars in the Chart confirmation		
			if (BarsInProgress != 0) 
			{
				return;
			}
			if (CurrentBars[0] < 1)
			{
				return;
			}
			
			LastMaxHighSwingHigh4 = MaxHighSwingHigh4;
			LastMaxHighSwingHigh14 = MaxHighSwingHigh14;
			LastMinLowSwingLow4 = MinLowSwingLow4;
			LastMinLowSwingLow14 = MinLowSwingLow14;
						 			
////		Reset isBO variable once a new swing comes up	
			if (CurrentSwingHigh4 != iSwing4.SwingHigh[0])
			{
				isBOUpSwing4 = false;
			}
			if (CurrentSwingHigh14 != iSwing14.SwingHigh[0])
			{
				isBOUpSwing14 = false;
			}
			if (CurrentSwingLow4 != iSwing4.SwingLow[0])
			{
				isBODownSwing4 = false;
			}
			if (CurrentSwingLow14 != iSwing14.SwingLow[0])
			{
				isBODownSwing14 = false;
			}
			
////		Variable setting for recurrent data sources				
			CurrentSwingHigh4 = iSwing4.SwingHigh[0];	
			CurrentSwingLow4 = iSwing4.SwingLow[0];
			CurrentSwingHigh14 = iSwing14.SwingHigh[0];
			CurrentSwingLow14 = iSwing14.SwingLow[0];
			CurrentATR = iATR[0];
			CurrentStop = CurrentATR * ATRStopFactor;
			CurrentOpen = Open[0];
			CurrentClose = Close[0];		
			CurrentHigh = High[0];
			CurrentLow = Low[0];
		
////		isLong, isShort reseting 		
			if (Position.MarketPosition == MarketPosition.Flat) //if the postition is stil active and the initial stop and trailing stop trigger price levels were set then...
			{
				isLong = false;
				isShort = false;
			}

////		SMA 50-200 Distance Method Calling 	
			SMAdif = SMADifferenceABS(iSMA200[0], iSMA50[0]); //Calculates the Distance Between the SMA 50-200			
			
////		Identifying the overall market  movement type Process and saving the ATR value at that time (SMA 50-200 Crossing Event) (if the SMA 50 is above the SMA 200 then there is an upward overall movement type, if the SMA 50 is below the SMA 200 then there is an downward overall movement type)	
			///Above
			if (CrossAbove(iSMA50, iSMA200, 1) && IsDownWard) //Once an SMAs 50-200 crossabove happens and there exists a downward overall movement then...
			{
				ATRCrossingTime = iATR[0]; //Tomar valor del ATR en el cruce
				IsUpWard = true; //Defining overall movement type
				IsDownWard = false; //Opposing movement Flag Lowering
				CrossAboveBar = CurrentBar; //Saves the bar number at the crossing time
			}
			
			///Below
			else if (CrossBelow(iSMA50, iSMA200, 1) && IsUpWard) //Once an SMAs 50-200 crossbelow happens and there exists an upward overall movement then...
			{
				ATRCrossingTime = iATR[0]; //Tomar valor ATR en el cruce
				IsDownWard = true; //Defining overall movement type
				IsUpWard = false; //Opposing movement Flag Lowering
				CrossBelowBar = CurrentBar; //Saves the bar number at the crossing time
			}
			
////		IncipientTrend Identification Process (IncipientTrend: there has been an SMAs 50-200 crossing event and the distance between those SMAs has reached the double of the ATR value at the crossing time)
			///UpWard			
			if (IsUpWard && SMAdif >= IncipientTrandFactor * ATRCrossingTime && ATRCrossingTime!=0) //Once an upward overall movement has been identified, the SMAs 50-200 distance is greater than the ATR value at the crossing time and there have been a first CrossAbove 50-200 event that records an ATR value then...
			{
				isIncipientUpTrend = true; //isIncipientUpTrend turning on
				isIncipientDownTrend = false; //isIncipientDownTrend turning off
				GrayEllipseShort = false; // GrayEllipseShort Flag Lowering
			}
			
			///DownWard		
			else if (IsDownWard && SMAdif >= IncipientTrandFactor * ATRCrossingTime && ATRCrossingTime!=0) //Once an downward overall movement has been identified, the SMAs 50-200 distance is greater than the ATR value at the crossing time and there have been a first CrossBelow 50-200 event that records an ATR value then...
			{
				isIncipientDownTrend = true; //isIncipientDownTrend turning on
				isIncipientUpTrend = false; //isIncipientUpTrend turning off
				GrayEllipseLong = false; //GrayEllipseLong Flag Lowering
			}
			
////		GrayEllipse turn on/off inside an incipient trend (SMA 20-50 Crossing Event) (GrayEllipse: An SMAs 20-50 crossing event in the opposite direction of the overall market movement)
			///Long			
			
			if (CrossBelow(iSMA20, iSMA50, 1))
			 {
			 	CrossBelow20_50 = true;
			 }
			if (isIncipientUpTrend && CrossBelow20_50) // If the SMA 50 is above the SMA200 (upward market movement), there is an SMAs 20-50 crossbelow event and the GrayEllipse in that direction is turned off (false) then...
			{
				if (isReentryLong)
				{
					isReentryLong = false;
				}
				GrayEllipseLong = true; //GrayEllipseLong Flag turning on
				int ArrayListSize = CurrentBar - CrossAboveBar; //Calculating the distance in bars (that determines the array size to check for the max/min swinghigh/low level) between the SMAs 20-50 crossing event and the Last SMAs 50-200 crossing event			
				if (ArrayListSize>=240) //trick to avoid the bug when trying to find the value of swing indicator beyond the 256 MaximunBarlookbar period, which is not possible
				{
					ArrayListSize=240;	
				}							
				SwingHigh14max = iSwing14.SwingHigh[0]; //initializing the variable that is going to keep the reference high, with the array firts value for comparison purposes
				for (int i = 0; i <= ArrayListSize; i++) //Loop that walk the array 
            	{
					if (iSwing14.SwingHigh[i] > SwingHigh14max && iSwing14.SwingHigh[i] != 0) //To determine the highest value (highest swinghigh 14)
                	{
						SwingHigh14max = iSwing14.SwingHigh[i]; //And saves that value in this variable
					}
				}	
				CrossBelow20_50 = false;
			}	
			else if (iSwing14.SwingHigh[0] > SwingHigh14max && GrayEllipseLong) //if the high of the current bar surpass the Max Level of the swinghigh (strength 14) where the GrayEllipseLong was originated 
			{
				SwingHigh14maxReentry = iSwing14.SwingHigh[0]; //Then lower the flag of the GrayEllipse in that direction
				isReentryLong = true;
			}	
			if (High[0] > SwingHigh14maxReentry && isReentryLong) //if the high of the current bar surpass the Max Level of the swinghigh (strength 14) where the GrayEllipseLong was originated 
			{
				GrayEllipseLong = false; //Then lower the flag of the GrayEllipse in that direction
				isReentryLong = false;
			}

			///Short
			if (CrossAbove(iSMA20, iSMA50, 1))
			 {
			 	CrossAbove20_50 = true;
			 }
			if (isIncipientDownTrend && CrossAbove20_50/* && !GrayEllipseShort*/) // If the SMA 50 is below the SMA200 (downward market movement), there is an SMAs 20-50 crabove event and the GrayEllipse in that direction is turned off (false) then...
			{
				if (isReentryShort)
				{
					isReentryShort = false;
				}
				GrayEllipseShort = true; //GrayEllipseLong Flag turning on
				int ArrayListSize = CurrentBar - CrossBelowBar; //Calculating the distance in bars (that determines the array size to check for the max/min swinghigh/low level) between the SMAs 20-50 crossing event and the Last SMAs 50-200 crossing event			
				if (ArrayListSize>=240) //trick to avoid the bug when trying to find the value of swing indicator beyond the 256 MaximunBarlookbar period, which is not possible
				{
					ArrayListSize=240;					
				}	
				SwingLow14min = iSwing14.SwingLow[0]; //initializing the variable that is going to keep the reference low, with the array firts value for comparison purposes
				for (int i = 0; i <= ArrayListSize; i++) //Loop that walk the array
            	{
					if (iSwing14.SwingLow[i] < SwingLow14min && iSwing14.SwingLow[i] != 0) //To determine the lowest value (lowest swinglow 14)
					{
                		SwingLow14min = iSwing14.SwingLow[i]; //And saves that value in this variable
					}
				}
				CrossAbove20_50 = false;
			}		
			else if (iSwing14.SwingLow[0] < SwingLow14min  && GrayEllipseShort) //if the Low of the current bar surpass the Min Level of the swinglow (strength 14) where the GrayEllipseShort was originated
			{
				SwingLow14minReentry = iSwing14.SwingLow[0]; //Then lower the flag of the GrayEllipse in that direction
				isReentryShort = true;
			}		
			if (Low[0] < SwingLow14minReentry  && isReentryShort) //if the Low of the current bar surpass the Min Level of the swinglow (strength 14) where the GrayEllipseShort was originated
			{
				GrayEllipseShort = false; //Then lower the flag of the GrayEllipse in that direction
				isReentryShort = false;
			}			

////		TRADE IDENTIFICATION			
////		Red Trade Type Process			
			///Long			 
			if (isIncipientDownTrend && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)) //if the overall market movement is upward and there is no active position then...
			{
				///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4				
				bool isSwingHigh4 = false; //Higher Swing strength 4 Flag creation, set to false and pending of validation
				bool isActiveLongPosition = false;
				if (iSwing4.SwingHigh[0] >= iSMA200[0] - iATR[0] * ClosnessToTrade && ((iSwing4.SwingHigh[0] + TicksToBO * TickSize) - iSMA200[0] <= CurrentStop || (iSwing4.SwingHigh[0] + TicksToBO * TickSize) - iSMA50[0] <= CurrentStop)) // If the reference Swing High 4 is above the SMA 50 then...
				{			
					Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA200, "High");
					isSwingHigh4 = ReturnedValues.Item1;
					isActiveLongPosition = ReturnedValues.Item2;
					isBOUpSwing4 = ReturnedValues.Item3;
				}				
				
				///Validate whether there is a valid higher low strength 14 (HL14) in case there is no valid HL4 and if so then send a long stop order above the reference swing high 14				
				if (iSwing14.SwingHigh[0] >= iSMA200[0] - iATR[0] * ClosnessToTrade && !isSwingHigh4 && !isActiveLongPosition && ((iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA200[0] <= CurrentStop || (iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA50[0] <= CurrentStop)) // If the reference Swing High 14 is above the SMA 50 and there is no HL4 then...
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA200, "High");
					bool isSwingHigh14 = ReturnedValues.Item1;
					isActiveLongPosition = ReturnedValues.Item2;
					isBOUpSwing14 = ReturnedValues.Item3;
				}				
			}
			 
			///Short		 
			else if (isIncipientUpTrend && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)) //if the overall market movement is upward and there is no active position then...
			{
				///Validate whether there is a valid lower high strength 4 (LH4) and if so then send a short stop order below the reference swing low 4			
				bool isSwingLow4 = false; //Lower Swing strength 4 Flag creation, set to false and pending of validation
				bool isActiveShortPosition = false;
				if (iSwing4.SwingLow[0] <= iSMA200[0] + iATR[0] * ClosnessToTrade && (iSMA200[0] - (iSwing4.SwingLow[0] - TicksToBO * TickSize) <= CurrentStop || iSMA50[0] - (iSwing4.SwingLow[0] - TicksToBO * TickSize) <= CurrentStop))
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA200, "Low");
					isSwingLow4 = ReturnedValues.Item1;
					isActiveShortPosition = ReturnedValues.Item2;
					isBODownSwing4 = ReturnedValues.Item3;
				}
				
				///Validate whether there is a valid lower high strength 14 (LH14) in case there is no valid LH4 and if so then send a short stop order below the reference swing low 14			
				if (iSwing14.SwingLow[0] <= iSMA200[0] + iATR[0] * ClosnessToTrade && !isSwingLow4 && !isActiveShortPosition && (iSMA200[0] - (iSwing14.SwingLow[0] - TicksToBO * TickSize) <= CurrentStop || iSMA50[0] - (iSwing14.SwingLow[0] - TicksToBO * TickSize) <= CurrentStop))
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA200, "Low");
					bool isSwingLow14 = ReturnedValues.Item1;
					isActiveShortPosition = ReturnedValues.Item2;
					isBODownSwing14 = ReturnedValues.Item3;
				}
			}
			
////		TRADITIONAL RED Trade Type Process			
			///Long			 
			if (isIncipientUpTrend && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)) //if the overall market movement is upward and there is no active position then...
			{
				///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4				
				bool isSwingHigh4 = false; //Higher Swing strength 4 Flag creation, set to false and pending of validation
				bool isActiveLongPosition = false;
				if (iSwing4.SwingHigh[0] >= iSMA200[0] - iATR[0] * ClosnessToTrade && (iSwing4.SwingHigh[0] + TicksToBO * TickSize) - iSMA200[0] <= CurrentStop && iSMA50[0] - iSwing4.SwingHigh[0] > iATR[0] * ClosnessFactor) // If the reference Swing High 4 is above the SMA 50 then...
				{			
					Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA200, "High");
					isSwingHigh4 = ReturnedValues.Item1;
					isActiveLongPosition = ReturnedValues.Item2;
					isBOUpSwing4 = ReturnedValues.Item3;
				}				
				
				///Validate whether there is a valid higher low strength 14 (HL14) in case there is no valid HL4 and if so then send a long stop order above the reference swing high 14				
				if (iSwing14.SwingHigh[0] >= iSMA200[0] - iATR[0] * ClosnessToTrade && !isSwingHigh4 && !isActiveLongPosition && (iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA200[0] <= CurrentStop && iSMA50[0] - iSwing14.SwingHigh[0] > iATR[0] * ClosnessFactor) // If the reference Swing High 14 is above the SMA 50 and there is no HL4 then...
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA200, "High");
					bool isSwingHigh14 = ReturnedValues.Item1;
					isActiveLongPosition = ReturnedValues.Item2;
					isBOUpSwing14 = ReturnedValues.Item3;
				}				
			}
			 
			///Short		 
			if (isIncipientDownTrend && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)) //if the overall market movement is upward and there is no active position then...
			{
				///Validate whether there is a valid lower high strength 4 (LH4) and if so then send a short stop order below the reference swing low 4			
				bool isSwingLow4 = false; //Lower Swing strength 4 Flag creation, set to false and pending of validation
				bool isActiveShortPosition = false;
				if (iSwing4.SwingLow[0] <= iSMA200[0] + iATR[0] * ClosnessToTrade && iSMA200[0] - (iSwing4.SwingLow[0] - TicksToBO * TickSize) <= CurrentStop && iSwing4.SwingLow[0] - iSMA50[0] > iATR[0] * ClosnessFactor)
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA200, "Low");
					isSwingLow4 = ReturnedValues.Item1;
					isActiveShortPosition = ReturnedValues.Item2;
					isBODownSwing4 = ReturnedValues.Item3;
				}
				
				///Validate whether there is a valid lower high strength 14 (LH14) in case there is no valid LH4 and if so then send a short stop order below the reference swing low 14			
				if (iSwing14.SwingLow[0] <= iSMA200[0] + iATR[0] * ClosnessToTrade && !isSwingLow4 && !isActiveShortPosition && iSMA200[0] - (iSwing14.SwingLow[0] - TicksToBO * TickSize) <= CurrentStop && iSwing14.SwingLow[0] - iSMA50[0] > iATR[0] * ClosnessFactor)
				{
					Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA200, "Low");
					bool isSwingLow14 = ReturnedValues.Item1;
					isActiveShortPosition = ReturnedValues.Item2;
					isBODownSwing14 = ReturnedValues.Item3;
				}
			}
			
////		Traditional Trade Type Process
			///Long
			if (isIncipientUpTrend && GrayEllipseLong && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Short)) //if we have an isIncipientUpTrend, a GrayEllipseLong and there is no active position then...
			{			
				///Normal traditional
				if (IsUpWard)
				{			
					///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4				
					bool isSwingHigh4 = false; //Higher Swing strength 4 Flag creation, set to false and pending of validation
					bool isActiveLongPosition = false;
					if (iSwing4.SwingHigh[0] >= iSMA50[0] - iATR[0] * ClosnessToTrade && (iSwing4.SwingHigh[0] + TicksToBO * TickSize) - iSMA50[0] <= CurrentStop) // If the reference Swing High 4 is above the SMA 50 then...
					{	
						Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA50, "High");
						isSwingHigh4 = ReturnedValues.Item1;
						isActiveLongPosition = ReturnedValues.Item2;
						isBOUpSwing4 = ReturnedValues.Item3;					
					}					
					
					///Validate whether there is a valid higher low strength 14 (HL14) in case there is no valid HL4 and if so then send a long stop order above the reference swing high 14					
					if (iSwing14.SwingHigh[0] >= iSMA50[0] - iATR[0] * ClosnessToTrade && !isSwingHigh4 && !isActiveLongPosition && (iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA50[0] <= CurrentStop) // If the reference Swing High 14 is above the SMA 50 and there is no HL4 then...
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA50, "High");
						bool isSwingHigh14 = ReturnedValues.Item1;
						isActiveLongPosition = ReturnedValues.Item2;
						isBOUpSwing14 = ReturnedValues.Item3;
					}
				}
				///Modified traditional
				else
				{
					///Validate whether there is a valid higher low strength 4 (HL4) and if so then send a long stop order above the reference swing high 4			
					bool isSwingHigh4 = false; //Higher Swing strength 4 Flag creation, set to false and pending of validation
					bool isActiveLongPosition = false;
					if (iSwing4.SwingHigh[0] >= iSMA200[0] - iATR[0] * ClosnessToTrade && (iSwing4.SwingHigh[0] + TicksToBO * TickSize) - iSMA200[0] <= CurrentStop) // If the reference Swing High 4 is above the SMA 50 then...
					{				
						Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA200, "High");
						isSwingHigh4 = ReturnedValues.Item1;
						isActiveLongPosition = ReturnedValues.Item2;
						isBOUpSwing4 = ReturnedValues.Item3;
					}				
					
					///Validate whether there is a valid higher low strength 14 (HL14) in case there is no valid HL4 and if so then send a long stop order above the reference swing high 14				
					if (iSwing14.SwingHigh[0] >= iSMA200[0] - iATR[0] * ClosnessToTrade && !isSwingHigh4 && !isActiveLongPosition && (iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA200[0] <= CurrentStop) // If the reference Swing High 14 is above the SMA 50 and there is no HL4 then...
					{
						
						Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA200, "High");
						bool isSwingHigh14 = ReturnedValues.Item1;
						isActiveLongPosition = ReturnedValues.Item2;
						isBOUpSwing14 = ReturnedValues.Item3;
					}
				}		
			}
			
			///Short	
			else if (isIncipientDownTrend && GrayEllipseShort && (Position.MarketPosition == MarketPosition.Flat || Position.MarketPosition == MarketPosition.Long)) //if we have an isIncipientDownTrend, a GrayEllipseShort and there is no active position then...
			{			
				///Normal traditional
				if (IsDownWard)	
				{				
					///Validate whether there is a valid lower high strength 4 (LH4) and if so then send a short stop order below the reference swing low 4		
					bool isSwingLow4 = false; //Lower Swing strength 4 Flag creation, set to false and pending of validation
					bool isActiveShortPosition = false;
					if (iSwing4.SwingLow[0] <= iSMA50[0] + iATR[0] * ClosnessToTrade && iSMA50[0] - (iSwing4.SwingLow[0] - TicksToBO * TickSize) <= CurrentStop)
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA50, "Low");
						isSwingLow4 = ReturnedValues.Item1;
						isActiveShortPosition = ReturnedValues.Item2;
						isBODownSwing4 = ReturnedValues.Item3;
					}					
					///Validate whether there is a valid lower high strength 14 (LH14) in case there is no valid LH4 and if so then send a short stop order below the reference swing low 14				
					if (iSwing14.SwingLow[0] <= iSMA50[0] + iATR[0] * ClosnessToTrade && !isSwingLow4 && !isActiveShortPosition && iSMA50[0] - (iSwing14.SwingLow[0] - TicksToBO * TickSize) <= CurrentStop)
					{	
						Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA50, "Low");
						bool isSwingLow14 = ReturnedValues.Item1;
						isActiveShortPosition = ReturnedValues.Item2;
						isBODownSwing14 = ReturnedValues.Item3;
					}
				}
				///Modified traditional
				else
				{
					///Validate whether there is a valid lower high strength 4 (LH4) and if so then send a short stop order below the reference swing low 4			
					bool isSwingLow4 = false; //Lower Swing strength 4 Flag creation, set to false and pending of validation
					bool isActiveShortPosition = false;
					if (iSwing4.SwingLow[0] <= iSMA200[0] + iATR[0] * ClosnessToTrade && iSMA200[0] - (iSwing4.SwingLow[0] - TicksToBO * TickSize) <= CurrentStop)
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing4(iSMA200, "Low");
						isSwingLow4 = ReturnedValues.Item1;
						isActiveShortPosition = ReturnedValues.Item2;
						isBODownSwing4 = ReturnedValues.Item3;
					}				
					///Validate whether there is a valid lower high strength 14 (LH14) in case there is no valid LH4 and if so then send a short stop order below the reference swing low 14				
					if (iSwing14.SwingLow[0] <= iSMA200[0] + iATR[0] * ClosnessToTrade && !isSwingLow4 && !isActiveShortPosition && iSMA200[0] - (iSwing14.SwingLow[0] - TicksToBO * TickSize) <= CurrentStop)
					{
						Tuple<bool, bool, bool> ReturnedValues = Swing14(iSMA200, "Low");
						bool isSwingLow14 = ReturnedValues.Item1;
						isActiveShortPosition = ReturnedValues.Item2;
						isBODownSwing14 = ReturnedValues.Item3;
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
				FixAmountLong = AmountLong;
				FixStopPriceLong = StopPriceLong;
				FixTriggerPriceLong = TriggerPriceLong;		
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
						if (High[BarsSinceEntryExecution(0, @"entryOrder", 0)] < iSMA50[BarsSinceEntryExecution(0, @"entryOrder", 0)] + TicksToBO * TickSize)
						{
							bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i=0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
							{
								if (High[i] >= iSMA50[i]) //To determine the highest value (max close value of the swinglow4)
								{
					        		HighCrossEma50 = true; //And saves that value in this variable
									break;
								}								
							}								
							if (HighCrossEma50 == false)
							{
								if (Close[0] < iSMA200[0] - iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryOrder");
								}
							}
							else
							{
								if (Close[0] < iSMA50[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA200[0] - iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryOrder");
								}
							}
						}
						else
						{
							if (Close[0] < iSMA50[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA200[0] - iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(FixAmountLong, @"exit", @"entryOrder");
							}
						}
						if (High[0] >= FixTriggerPriceLong)
						{
							FixStopPriceLong = High[0] - CurrentStop * TrailerUnitsStop;
							FixTriggerPriceLong = High[0];
							ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
							Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
						}
					}
					else if (myEntryOrder == null && myEntryMarket.OrderState == OrderState.Filled)
					{
						ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryMarket");
	//						}
	//					}				
						if (High[BarsSinceEntryExecution(0, @"entryMarket", 0)] < iSMA50[BarsSinceEntryExecution(0, @"entryMarket", 0)] + TicksToBO * TickSize)
						{
							bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i=0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
							{
								if (High[i] >= iSMA50[i]) //To determine the highest value (max close value of the swinglow4)
								{
					        		HighCrossEma50 = true; //And saves that value in this variable
									break;
								}								
							}								
							if (HighCrossEma50 == false)
							{
								if (Close[0] < iSMA200[0] - iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryMarket");
								}
							}
							else
							{
								if (Close[0] < iSMA50[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA200[0] - iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryMarket");
								}
							}
						}
						else
						{
							if (Close[0] < iSMA50[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA200[0] - iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(FixAmountLong, @"exit", @"entryMarket");
							}
						}
						if (High[0] >= FixTriggerPriceLong)
						{
							FixStopPriceLong = High[0] - CurrentStop * TrailerUnitsStop;
							FixTriggerPriceLong = High[0];
							ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryMarket");
							Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
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
					if (High[BarsSinceEntryExecution(0, @"entryOrder", 0)] < iSMA50[BarsSinceEntryExecution(0, @"entryOrder", 0)] + TicksToBO * TickSize)
						{
							bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i=0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
							{
								if (High[i] >= iSMA50[i]) //To determine the highest value (max close value of the swinglow4)
								{
					        		HighCrossEma50 = true; //And saves that value in this variable
									break;
								}								
							}								
							if (HighCrossEma50 == false)
							{
								if (Close[0] < iSMA200[0] - iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryOrder");
								}
							}
							else
							{
								if (Close[0] < iSMA50[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA200[0] - iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
									ExitLong(FixAmountLong, @"exit", @"entryOrder");
								}
							}
						}
						else
						{
							if (Close[0] < iSMA50[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA200[0] - iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(FixAmountLong, @"exit", @"entryOrder");
							}
						}
					if (High[0] >= FixTriggerPriceLong)
					{
						FixStopPriceLong = High[0] - CurrentStop * TrailerUnitsStop;
						FixTriggerPriceLong = High[0];
						ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryOrder");
						Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
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
					if (High[BarsSinceEntryExecution(0, @"entryMarket", 0)] < iSMA50[BarsSinceEntryExecution(0, @"entryMarket", 0)] + TicksToBO * TickSize)
					{
						bool HighCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i=0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
						{
							if (High[i] >= iSMA50[i]) //To determine the highest value (max close value of the swinglow4)
							{
				        		HighCrossEma50 = true; //And saves that value in this variable
								break;
							}								
						}								
						if (HighCrossEma50 == false)
						{
							if (Close[0] < iSMA200[0] - iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(FixAmountLong, @"exit", @"entryMarket");
							}
						}
						else
						{
							if (Close[0] < iSMA50[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA200[0] - iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
								ExitLong(FixAmountLong, @"exit", @"entryMarket");
							}
						}
					}
					else
					{
						if (Close[0] < iSMA50[0] - iATR[0] * (MagicNumber / 100) || Close[0] < iSMA200[0] - iATR[0] * (MagicNumber / 100))
						{
							Draw.ArrowUp(this, @"SMAStopBar_GreenArrowUp" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Lime);
							ExitLong(FixAmountLong, @"exit", @"entryMarket");
						}
					}
					if (High[0] >= FixTriggerPriceLong)
					{
						FixStopPriceLong = High[0] - CurrentStop * TrailerUnitsStop;
						FixTriggerPriceLong = High[0];
						ExitLongStopMarket(FixAmountLong, FixStopPriceLong, @"exit", @"entryMarket");
						Draw.Diamond(this, @"TrailingStopBar_GreenDiamond" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
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
						if (Low[BarsSinceEntryExecution(0, @"entryOrder", 0)] > iSMA50[BarsSinceEntryExecution(0, @"entryOrder", 0)] - TicksToBO * TickSize)
						{
							bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i=0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
							{
								if (Low[i] <= iSMA50[i]) //To determine the highest value (max close value of the swinglow4)
								{
					        		LowCrossEma50 = true; //And saves that value in this variable
									break;
								}								
							}								
							if (LowCrossEma50 == false)
							{
								if (Close[0] > iSMA200[0] + iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
									ExitShort(FixAmountShort, @"exit", @"entryOrder");
								}
							}
							else
							{
								if (Close[0] > iSMA50[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA200[0] + iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
									ExitShort(FixAmountShort, @"exit", @"entryOrder");
								}
							}
						}
						else
						{
							if (Close[0] > iSMA50[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA200[0] + iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(FixAmountShort, @"exit", @"entryOrder");
							}
						}
						if (Low[0] <= FixTriggerPriceShort)
						{
							FixStopPriceShort = Low[0] + CurrentStop * TrailerUnitsStop;
							FixTriggerPriceShort = Low[0];
							ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryOrder");
							Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Red);
						}
					}
					else if (myEntryOrder == null && myEntryMarket.OrderState == OrderState.Filled)
					{
						ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryMarket");
//						}
//					}
						if (Low[BarsSinceEntryExecution(0, @"entryMarket", 0)] > iSMA50[BarsSinceEntryExecution(0, @"entryMarket", 0)] - TicksToBO * TickSize)
						{
							bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
							for (int i=0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
							{
								if (Low[i] <= iSMA50[i]) //To determine the highest value (max close value of the swinglow4)
								{
					        		LowCrossEma50 = true; //And saves that value in this variable
									break;
								}								
							}								
							if (LowCrossEma50 == false)
							{
								if (Close[0] > iSMA200[0] + iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
									ExitShort(FixAmountShort, @"exit", @"entryMarket");
								}
							}
							else
							{
								if (Close[0] > iSMA50[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA200[0] + iATR[0] * (MagicNumber / 100))
								{
									Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
									ExitShort(FixAmountShort, @"exit", @"entryMarket");
								}
							}
						}
						else
						{
							if (Close[0] > iSMA50[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA200[0] + iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(FixAmountShort, @"exit", @"entryMarket");
							}
						}
						if (Low[0] <= FixTriggerPriceShort)
						{
							FixStopPriceShort = Low[0] + CurrentStop * TrailerUnitsStop;
							FixTriggerPriceShort = Low[0];
							ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryMarket");
							Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Red);
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
					if (Low[BarsSinceEntryExecution(0, @"entryOrder", 0)] > iSMA50[BarsSinceEntryExecution(0, @"entryOrder", 0)] - TicksToBO * TickSize)
					{
						bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i=0; i <= BarsSinceEntryExecution(0, @"entryOrder", 0); i++) // Walks every bar of the potential HL4
						{
							if (Low[i] <= iSMA50[i]) //To determine the highest value (max close value of the swinglow4)
							{
				        		LowCrossEma50 = true; //And saves that value in this variable
								break;
							}								
						}								
						if (LowCrossEma50 == false)
						{
							if (Close[0] > iSMA200[0] + iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(FixAmountShort, @"exit", @"entryOrder");
							}
						}
						else
						{
							if (Close[0] > iSMA50[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA200[0] + iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(FixAmountShort, @"exit", @"entryOrder");
							}
						}
					}
					else
					{
						if (Close[0] > iSMA50[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA200[0] + iATR[0] * (MagicNumber / 100))
						{
							Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
							ExitShort(FixAmountShort, @"exit", @"entryOrder");
						}
					}
					if (Low[0] <= FixTriggerPriceShort)
					{
						FixStopPriceShort = Low[0] + CurrentStop * TrailerUnitsStop;
						FixTriggerPriceShort = Low[0];
						ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryOrder");
						Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Red);
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
					if (Low[BarsSinceEntryExecution(0, @"entryMarket", 0)] > iSMA50[BarsSinceEntryExecution(0, @"entryMarket", 0)] - TicksToBO * TickSize)
					{
						bool LowCrossEma50 = false; //initializing the variable that is going to keep the min close value of the swing, with the array firts value for comparison purposes
						for (int i=0; i <= BarsSinceEntryExecution(0, @"entryMarket", 0); i++) // Walks every bar of the potential HL4
						{
							if (Low[i] <= iSMA50[i]) //To determine the highest value (max close value of the swinglow4)
							{
				        		LowCrossEma50 = true; //And saves that value in this variable
								break;
							}								
						}								
						if (LowCrossEma50 == false)
						{
							if (Close[0] > iSMA200[0] + iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(FixAmountShort, @"exit", @"entryMarket");
							}
						}
						else
						{
							if (Close[0] > iSMA50[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA200[0] + iATR[0] * (MagicNumber / 100))
							{
								Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
								ExitShort(FixAmountShort, @"exit", @"entryMarket");
							}
						}
					}
					else
					{
						if (Close[0] > iSMA50[0] + iATR[0] * (MagicNumber / 100) || Close[0] > iSMA200[0] + iATR[0] * (MagicNumber / 100))
						{
							Draw.ArrowDown(this, @"SMAStopBar_RedArrowDown" + CurrentBar, true, 0, High[0] + 3 * TicksToBO * TickSize, Brushes.Red);
							ExitShort(FixAmountShort, @"exit", @"entryMarket");
						}
					}
					if (Low[0] <= FixTriggerPriceShort)
					{
						FixStopPriceShort = Low[0] + CurrentStop * TrailerUnitsStop;
						FixTriggerPriceShort = Low[0];
						ExitShortStopMarket(FixAmountShort, FixStopPriceShort, @"exit", @"entryMarket");
						Draw.Diamond(this, @"TrailingStopBar_RedDiamond" + CurrentBar, true, 0, Low[0] - 3 * TicksToBO * TickSize, Brushes.Red);
					}
				}
			}
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SMA1", Order=1, GroupName="Parameters")]
		public int SMA1
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SMA2", Order=2, GroupName="Parameters")]
		public int SMA2
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SMA3", Order=3, GroupName="Parameters")]
		public int SMA3
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="UnitsTriggerForTrailing", Order=4, GroupName="Parameters")]
		public double UnitsTriggerForTrailing
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="TrailerUnitsStop", Order=5, GroupName="Parameters")]
		public double TrailerUnitsStop
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
		[Display(Name="IncipientTrandFactor", Order=10, GroupName="Parameters")]
		public double IncipientTrandFactor
		{ get; set; }
				
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="ATRStopFactor", Order=11, GroupName="Parameters")]
		public double ATRStopFactor
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="TicksToBO", Order=12, GroupName="Parameters")]
		public double TicksToBO
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, 100)]
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
		#endregion
		
////	METHODS			
////	SMA 50-200 Distance Calculate Method		
		public double SMADifferenceABS(double SMAa, double SMAb) //Valor absoluto de la diferencia entre SMAs
        {
			return Math.Abs(SMAa - SMAb);
		}
		
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
		public Tuple<double, double, int> SwingLocation(ISeries<double> BarExtreme, ISeries<double> ReferenceSwing, int ReferenceSwingBar, string SwingType) 
		{	
			double SwingMidLevel = ReferenceSwing[0];
			double ExtremeLevel = BarExtreme[0]; //initializing the variable that is going to keep the max high value of the swing, with the array firts value for comparison purposes
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
			}
			return new Tuple<double, double, int>(ExtremeLevel, SwingMidLevel, ExtremeLevelBar);	
		}
		
////	Buy Process	Method		
		public bool Buy(string OrderType, ISeries<double> BOLevel) 
		{
			if (OrderType == "MarketOrder")
			{
				AmountLong = Convert.ToInt32((RiskUnit / ((CurrentStop / TickSize) * Instrument.MasterInstrument.PointValue * TickSize))); //calculates trade amount
				EnterLong(AmountLong, @"entryMarket"); // Long Stop order activation							
				StopPriceLong = Close[0] - CurrentStop; //calculates the stop price level
				TriggerPriceLong = Close[0] + CurrentStop * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger	
				return true;
			}
			else if (OrderType == "PendingOrder")
			{
				AmountLong = Convert.ToInt32((RiskUnit / ((CurrentStop / TickSize) * Instrument.MasterInstrument.PointValue * TickSize)));
				EnterLongStopMarket(AmountLong, BOLevel[0] + TicksToBO * TickSize, @"entryOrder");							
				StopPriceLong = (BOLevel[0] + TicksToBO * TickSize) - CurrentStop; //calculates the stop price level
				TriggerPriceLong = (BOLevel[0] + TicksToBO * TickSize) + CurrentStop * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger							
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
				AmountShort = Convert.ToInt32((RiskUnit / ((CurrentStop / TickSize) * Instrument.MasterInstrument.PointValue * TickSize))); //calculates trade amount
				EnterShort(AmountShort, @"entryMarket"); // Long Stop order activation							
				StopPriceShort = Close[0] + CurrentStop; //calculates the stop price level
				TriggerPriceShort = Close[0] - CurrentStop * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger	
				return true;
			}
			else if (OrderType == "PendingOrder")
			{
				AmountShort = Convert.ToInt32((RiskUnit / ((CurrentStop / TickSize) * Instrument.MasterInstrument.PointValue * TickSize)));
				EnterShortStopMarket(AmountShort, BOLevel[0] - TicksToBO * TickSize, @"entryOrder");							
				StopPriceShort = (BOLevel[0] - TicksToBO * TickSize) + CurrentStop; //calculates the stop price level
				TriggerPriceShort = (BOLevel[0] - TicksToBO * TickSize) - CurrentStop * UnitsTriggerForTrailing; //calculates the price level where the trailing stop is going to be trigger					
				Print (String.Format("{0} // {1} // {2} // {3}", AmountShort, BOLevel[0], StopPriceShort, Time[0]));
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
					MaxHighSwingHigh4 = ReturnedValues.Item1;
					double SwingHigh4MidLevel = ReturnedValues.Item2;
					int ExtremeLevelBar = ReturnedValues.Item3;
					if (SwingHigh4MidLevel >= ReferenceSMA[0] /*&& MaxHighSwingHigh4 < iSwing4.SwingHigh[0] + TicksToBO * TickSize*/)
					{
						if (iSwing14.SwingHigh[0] > iSwing4.SwingHigh[0] && iSwing14.SwingHigh[0] <= iSwing4.SwingHigh[0] + iATR[0] * ClosnessFactor)
						{
							if ((iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA200[0] <= CurrentStop || (iSwing14.SwingHigh[0] + TicksToBO * TickSize) - iSMA50[0] <= CurrentStop)
							{
								if (myEntryOrder != null && myExitOrder != null)
								{
									if ((myEntryOrder.OrderType == OrderType.StopMarket || myExitOrder.OrderType == OrderType.StopMarket) && (myEntryOrder.OrderState == OrderState.Working || myEntryOrder.OrderState == OrderState.Filled || myExitOrder.OrderState == OrderState.Working) && (myEntryOrder.OrderAction == OrderAction.SellShort || myExitOrder.OrderAction == OrderAction.SellShort))
									{	
										Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing14.SwingHigh, iSwing4.SwingHigh, ReferenceSMA, ExtremeLevelBar, isBOUpSwing4, MaxHighSwingHigh4, "Up");
										isBOUpSwing4 = ReturnedValues1.Item1;
										isActiveLongPosition = ReturnedValues1.Item2;
									}
									else
									{									
										if (MaxHighSwingHigh4 < iSwing14.SwingHigh[0] + TicksToBO * TickSize)
										{
											Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing14.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Lime); //Draw a mark to know where all conditions have been met
											isActiveLongPosition = Buy("PendingOrder", iSwing14.SwingHigh);
											 
										}
										else 
										{
											isBOUpSwing4 = true;
										}
									}
								}
								else
								{
									if (MaxHighSwingHigh4 < iSwing14.SwingHigh[0] + TicksToBO * TickSize)
									{
										Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing14.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Lime); //Draw a mark to know where all conditions have been met
										isActiveLongPosition = Buy("PendingOrder", iSwing14.SwingHigh);
										 
									}
									else 
									{
										isBOUpSwing4 = true;
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
									Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing4.SwingHigh, iSwing4.SwingHigh, ReferenceSMA, ExtremeLevelBar, isBOUpSwing4, MaxHighSwingHigh4, "Up");
									isBOUpSwing4 = ReturnedValues1.Item1;
									isActiveLongPosition = ReturnedValues1.Item2;
								}
								else
								{								
									if (MaxHighSwingHigh4 < iSwing4.SwingHigh[0] + TicksToBO * TickSize)
									{
										Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing4.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Lime); //Draw a mark to know where all conditions have been met
										isActiveLongPosition = Buy("PendingOrder", iSwing4.SwingHigh);
										 
									}
									else
									{
										isBOUpSwing4 = true;
									}
								}
							}
							else
							{
								if (MaxHighSwingHigh4 < iSwing4.SwingHigh[0] + TicksToBO * TickSize)
									{
										Draw.Dot(this, @"LimeDot" + CurrentBar, true, 0, iSwing4.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Lime); //Draw a mark to know where all conditions have been met
										isActiveLongPosition = Buy("PendingOrder", iSwing4.SwingHigh);
										 
									}
									else
									{
										isBOUpSwing4 = true;
									}
							}
						}
					}
					else if (iSwing4.SwingHigh[0] >= ReferenceSMA[0])	
					{
						if (iSwing14.SwingHigh[0] > iSwing4.SwingHigh[0] && iSwing14.SwingHigh[0] <= iSwing4.SwingHigh[0] + iATR[0] * ClosnessFactor)
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing14.SwingHigh, ReferenceSMA, MaxHighSwingHigh4, ExtremeLevelBar, 0, "Up", "Swing4", isBOUpSwing4);
							isBOUpSwing4 = ReturnedValues1.Item1;
							isActiveLongPosition = ReturnedValues1.Item2;
						}
						else
						{	
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing4(iSwing4.SwingHigh, ReferenceSMA, ExtremeLevelBar, 0, "Up");
							isBOUpSwing4 = ReturnedValues1.Item1;
							isActiveLongPosition = ReturnedValues1.Item2;
						}
					}
					else 
					{	
						if (iSwing14.SwingHigh[0] > iSwing4.SwingHigh[0] && iSwing14.SwingHigh[0] <= iSwing4.SwingHigh[0] + iATR[0] * ClosnessFactor)
						{
							if (iSwing14.SwingHigh[0] < ReferenceSMA[0])
							{	
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(ReferenceSMA, ReferenceSMA, MaxHighSwingHigh4, ExtremeLevelBar, ExtremeLevelBar, "Up", "Swing4", isBOUpSwing4);
								isBOUpSwing4 = ReturnedValues1.Item1;
								isActiveLongPosition = ReturnedValues1.Item2;
							}	
							else
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing14.SwingHigh, ReferenceSMA, MaxHighSwingHigh4, ExtremeLevelBar, 0, "Up", "Swing4", isBOUpSwing4);
								isBOUpSwing4 = ReturnedValues1.Item1;
								isActiveLongPosition = ReturnedValues1.Item2;
							}
						}
						else
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing4(ReferenceSMA, ReferenceSMA, ExtremeLevelBar, ExtremeLevelBar, "Up");
							isBOUpSwing4 = ReturnedValues1.Item1;
							isActiveLongPosition = ReturnedValues1.Item2;
						}
					}
				}
				if (isBOUpSwing4)
				{
					isSwingHigh4 = false;
				}
				return new Tuple<bool, bool, bool>(isSwingHigh4, isActiveLongPosition, isBOUpSwing4);
			}
			
////		SWING 4 LOW
			else if (Type == "Low")
			{
				bool isSwingLow4 = SwingIdentifiacation(Close, iSwing4.SwingHigh, iSwing4.SwingLowBar(0, 1, CurrentBar), true, "SwingLow");
				bool isActiveShortPosition = false;
				if (isSwingLow4)
				{
					Tuple<double, double, int> ReturnedValues = SwingLocation(Low, iSwing4.SwingLow, iSwing4.SwingLowBar(0, 1, CurrentBar), "SwingLow");
					MinLowSwingLow4 = ReturnedValues.Item1;
					double SwingLow4MidLevel = ReturnedValues.Item2;	
					int ExtremeLevelBar = ReturnedValues.Item3;
					if (SwingLow4MidLevel <= ReferenceSMA[0] /*&& MinLowSwingLow4 > iSwing4.SwingLow[0] - TicksToBO * TickSize*/)
					{
						if (iSwing14.SwingLow[0] < iSwing4.SwingLow[0] && iSwing14.SwingLow[0] >= iSwing4.SwingLow[0] - iATR[0] * ClosnessFactor)
						{
							if (iSMA200[0] - iSwing14.SwingLow[0] - TicksToBO * TickSize <= CurrentStop || iSMA50[0] - iSwing14.SwingLow[0] - TicksToBO * TickSize <= CurrentStop)
							{
								if (myEntryOrder != null && myExitOrder != null)
								{
									if ((myEntryOrder.OrderType == OrderType.StopMarket || myExitOrder.OrderType == OrderType.StopMarket) && (myEntryOrder.OrderState == OrderState.Working || myEntryOrder.OrderState == OrderState.Filled || myExitOrder.OrderState == OrderState.Working) && (myEntryOrder.OrderAction == OrderAction.Buy || myExitOrder.OrderAction == OrderAction.Buy))
									{
										Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing14.SwingLow, iSwing4.SwingLow, ReferenceSMA, ExtremeLevelBar, isBODownSwing4, MinLowSwingLow4, "Down");
										isBODownSwing4 = ReturnedValues1.Item1;
										isActiveShortPosition = ReturnedValues1.Item2;
									}
									else
									{									
										if (MinLowSwingLow4 > iSwing14.SwingLow[0] - TicksToBO * TickSize)
										{
											Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing14.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Red);
											isActiveShortPosition = Sell("PendingOrder", iSwing14.SwingLow);
										}
										else
										{
											isBODownSwing4 = true;
										}
									}
								}
								else
								{
									if (MinLowSwingLow4 > iSwing14.SwingLow[0] - TicksToBO * TickSize)
									{
										Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing14.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Red);
										isActiveShortPosition = Sell("PendingOrder", iSwing14.SwingLow);
									}
									else
									{
										isBODownSwing4 = true;
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
									Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing4.SwingLow, iSwing4.SwingLow, ReferenceSMA, ExtremeLevelBar, isBODownSwing4, MinLowSwingLow4, "Down");
									isBODownSwing4 = ReturnedValues1.Item1;
									isActiveShortPosition = ReturnedValues1.Item2;
								}
								else
								{									
									if (MinLowSwingLow4 > iSwing4.SwingLow[0] - TicksToBO * TickSize)
									{
										Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing4.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Red);
										isActiveShortPosition = Sell("PendingOrder", iSwing4.SwingLow);
									}
									else
									{
										isBODownSwing4 = true;
									}
								}
							}
							else
							{
								if (MinLowSwingLow4 > iSwing4.SwingLow[0] - TicksToBO * TickSize)
								{
									Draw.Dot(this, @"RedDot" + CurrentBar, true, 0, iSwing4.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Red);
									isActiveShortPosition = Sell("PendingOrder", iSwing4.SwingLow);
								}
								else
								{
									isBODownSwing4 = true;
								}
							}
						}
					} 
					else if (iSwing4.SwingLow[0] <= ReferenceSMA[0])
					{
						if (iSwing14.SwingLow[0] < iSwing4.SwingLow[0] && iSwing14.SwingLow[0] >= iSwing4.SwingLow[0] - iATR[0] * ClosnessFactor)
						{		
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing14.SwingLow, ReferenceSMA, MinLowSwingLow4, ExtremeLevelBar, 0, "Down", "Swing4", isBODownSwing4);
							isBODownSwing4 = ReturnedValues1.Item1;
							isActiveShortPosition = ReturnedValues1.Item2;
						}
						else
						{
//							Print (String.Format("{0} // {1} // {2} // {3}", isBODownSwing4, iSwing14.SwingLow[0], iSwing4.SwingLow[0], Time[0]));
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing4(iSwing4.SwingLow, ReferenceSMA, ExtremeLevelBar, 0, "Down");
							isBODownSwing4 = ReturnedValues1.Item1;
							isActiveShortPosition = ReturnedValues1.Item2;
						}
					}	
					else
					{
						if (iSwing14.SwingLow[0] < iSwing4.SwingLow[0] && iSwing14.SwingLow[0] >= iSwing4.SwingLow[0] - iATR[0] * ClosnessFactor)
						{																		
							if (iSwing14.SwingLow[0] > ReferenceSMA[0])
							{	
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(ReferenceSMA, ReferenceSMA, MinLowSwingLow4, ExtremeLevelBar, ExtremeLevelBar, "Down", "Swing4", isBODownSwing4);
								isBODownSwing4 = ReturnedValues1.Item1;
								isActiveShortPosition = ReturnedValues1.Item2;
							}
							else
							{
								Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing14.SwingLow, ReferenceSMA, MinLowSwingLow4, ExtremeLevelBar, 0, "Down", "Swing4", isBODownSwing4);
								isBODownSwing4 = ReturnedValues1.Item1;
								isActiveShortPosition = ReturnedValues1.Item2;
							}
						}
						else
						{
							Tuple<bool, bool> ReturnedValues1 = BOProofSwing4(ReferenceSMA, ReferenceSMA, ExtremeLevelBar, ExtremeLevelBar, "Down");
							isBODownSwing4 = ReturnedValues1.Item1;
							isActiveShortPosition = ReturnedValues1.Item2;							
						}
					}	
				}
				if (isBODownSwing4)
				{
					isSwingLow4 = false;
				}
				return new Tuple<bool, bool, bool>(isSwingLow4, isActiveShortPosition, isBODownSwing4);
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
					MaxHighSwingHigh14 = ReturnedValues.Item1;
					double SwingHigh14MidLevel = ReturnedValues.Item2;
					int ExtremeLevelBar = ReturnedValues.Item3;
					if (SwingHigh14MidLevel >= ReferenceSMA[0]  /*&& MaxHighSwingHigh14 < iSwing14.SwingHigh[0] + TicksToBO * TickSize*/)
					{	
						if (myEntryOrder != null && myExitOrder != null)
						{
							if ((myEntryOrder.OrderType == OrderType.StopMarket || myExitOrder.OrderType == OrderType.StopMarket) && (myEntryOrder.OrderState == OrderState.Working || myEntryOrder.OrderState == OrderState.Filled || myExitOrder.OrderState == OrderState.Working) && (myEntryOrder.OrderAction == OrderAction.SellShort || myExitOrder.OrderAction == OrderAction.SellShort))
							{
								Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing14.SwingHigh, iSwing14.SwingHigh, ReferenceSMA, ExtremeLevelBar, isBOUpSwing14, MaxHighSwingHigh14, "Up");
								isBOUpSwing14 = ReturnedValues1.Item1;
								isActiveLongPosition = ReturnedValues1.Item2;
							}
							else
							{
								
								if (MaxHighSwingHigh14 < iSwing14.SwingHigh[0] + TicksToBO * TickSize)
								{						
									Draw.Dot(this, @"GreenDot" + CurrentBar, true, 0, iSwing14.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Green);		
									isActiveLongPosition = Buy("PendingOrder", iSwing14.SwingHigh);
								}
								else
								{
									isBOUpSwing14 = true;
								}
							}
						}
						else
						{
							if (MaxHighSwingHigh14 < iSwing14.SwingHigh[0] + TicksToBO * TickSize)
							{						
								Draw.Dot(this, @"GreenDot" + CurrentBar, true, 0, iSwing14.SwingHigh[0] + 3 * TicksToBO * TickSize, Brushes.Green);		
								isActiveLongPosition = Buy("PendingOrder", iSwing14.SwingHigh);
							}
							else
							{
								isBOUpSwing14 = true;
							}
						}
					}
					else if (iSwing14.SwingHigh[0] >= ReferenceSMA[0])
					{
						Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing14.SwingHigh, ReferenceSMA, MaxHighSwingHigh14, ExtremeLevelBar, 0, "Up", "Swing14", isBOUpSwing14);
						isBOUpSwing14 = ReturnedValues1.Item1;
						isActiveLongPosition = ReturnedValues1.Item2;
					}
					else
					{
						Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(ReferenceSMA, ReferenceSMA, MaxHighSwingHigh14, ExtremeLevelBar, ExtremeLevelBar, "Up", "Swing14", isBOUpSwing14);
						isBOUpSwing14 = ReturnedValues1.Item1;
						isActiveLongPosition = ReturnedValues1.Item2;
					}
				}
				if (isBOUpSwing14)
				{
					isSwingHigh14 = false;
				}
				return new Tuple<bool, bool, bool>(isSwingHigh14, isActiveLongPosition, isBOUpSwing14);
			}
			
////		SWING 14 LOW
			else if (Type == "Low")
			{
				bool isSwingLow14 = SwingIdentifiacation(Close, iSwing14.SwingHigh, iSwing14.SwingLowBar(0, 1, CurrentBar), true, "SwingLow");
				bool isActiveShortPosition = false;
				if(isSwingLow14)
				{
					Tuple<double, double, int> ReturnedValues = SwingLocation(Low, iSwing14.SwingLow, iSwing14.SwingLowBar(0, 1, CurrentBar), "SwingLow");
					MinLowSwingLow14 = ReturnedValues.Item1;	
					double SwingLow14MidLevel = ReturnedValues.Item2;
					int ExtremeLevelBar = ReturnedValues.Item3;
					if (SwingLow14MidLevel <= ReferenceSMA[0] /*&& MinLowSwingLow14 > iSwing14.SwingLow[0] - TicksToBO * TickSize*/)
					{	
						if (myEntryOrder != null && myExitOrder != null)
						{
							if ((myEntryOrder.OrderType == OrderType.StopMarket || myExitOrder.OrderType == OrderType.StopMarket) && (myEntryOrder.OrderState == OrderState.Working || myEntryOrder.OrderState == OrderState.Filled || myExitOrder.OrderState == OrderState.Working) && (myEntryOrder.OrderAction == OrderAction.Buy || myExitOrder.OrderAction == OrderAction.Buy))
							{
								Tuple<bool, bool> ReturnedValues1 = MarketVSPending(iSwing14.SwingLow, iSwing14.SwingLow, ReferenceSMA, ExtremeLevelBar, isBODownSwing14, MinLowSwingLow14, "Down");
								isBODownSwing14 = ReturnedValues1.Item1;
								isActiveShortPosition = ReturnedValues1.Item2;
							}
							else
							{						
								if (MinLowSwingLow14 > iSwing14.SwingLow[0] - TicksToBO * TickSize)
								{					
									Draw.Dot(this, @"MaroonDot" + CurrentBar, true, 0, iSwing14.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Maroon);	
									isActiveShortPosition = Sell("PendingOrder", iSwing14.SwingLow);
								}
								else
								{
									isBODownSwing14 = true;
								}
							}
						}
						else
						{
							if (MinLowSwingLow14 > iSwing14.SwingLow[0] - TicksToBO * TickSize)
							{					
								Draw.Dot(this, @"MaroonDot" + CurrentBar, true, 0, iSwing14.SwingLow[0] - 3 * TicksToBO * TickSize, Brushes.Maroon);	
								isActiveShortPosition = Sell("PendingOrder", iSwing14.SwingLow);
							}
							else
							{
								isBODownSwing14 = true;
							}
						}
					} 
					else if (iSwing14.SwingLow[0] <= ReferenceSMA[0])
					{
						Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(iSwing14.SwingLow, ReferenceSMA, MinLowSwingLow14, ExtremeLevelBar, 0, "Down", "Swing14", isBODownSwing14);
						isBODownSwing14 = ReturnedValues1.Item1;
						isActiveShortPosition = ReturnedValues1.Item2;
					}
					else
					{
						Tuple<bool, bool> ReturnedValues1 = BOProofSwing14(ReferenceSMA, ReferenceSMA, MinLowSwingLow14, ExtremeLevelBar, ExtremeLevelBar, "Down", "Swing14", isBODownSwing14);
						isBODownSwing14 = ReturnedValues1.Item1;
						isActiveShortPosition = ReturnedValues1.Item2;
					}
				}
				if (isBODownSwing14)
				{
					isSwingLow14 = false;
				}
				return new Tuple<bool, bool, bool>(isSwingLow14, isActiveShortPosition, isBODownSwing14);
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
								if (Close[0] - ReferenceSMA[0] <= CurrentStop)
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
							if (Close[0] >= LastMaxHighSwingHigh4 + TicksToBO * TickSize)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, LastMaxHighSwingHigh4 + 3 * TicksToBO * TickSize, Brushes.Cyan);
								if (Close[0] - ReferenceSMA[0] <= CurrentStop)
								{
									isActiveLongPosition = Buy("MarketOrder", Close);
								}
							}
						}
						else if (Swing == "Swing14")
						{
							if (Close[0] >= LastMaxHighSwingHigh14 + TicksToBO * TickSize && ReferenceBar == 0)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, LastMaxHighSwingHigh14 + 3 * TicksToBO * TickSize, Brushes.Cyan);
								if (Close[0] - ReferenceSMA[0] <= CurrentStop)
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
								if (ReferenceSMA[0] - Close[0] <= CurrentStop)
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
							if (Close[0] <= LastMinLowSwingLow4 - TicksToBO * TickSize)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, LastMinLowSwingLow4 - 3 * TicksToBO * TickSize, Brushes.Cyan);
								if (ReferenceSMA[0] - Close[0] <= CurrentStop)
								{
									isActiveShortPosition = Sell("MarketOrder", Close);
								}
							}
						}
						else if (Swing == "Swing14")
						{
							if (Close[0] <= LastMinLowSwingLow14 - TicksToBO * TickSize && ReferenceBar == 0)
							{
								Draw.Square(this, @"CyanSquare" + CurrentBar, true, 0, LastMinLowSwingLow14 - 3 * TicksToBO * TickSize, Brushes.Cyan);
								if (ReferenceSMA[0] - Close[0] <= CurrentStop)
								{
									isActiveShortPosition = Sell("MarketOrder", Close);
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
				if (!isBOUpSwing4)
				{								
					if (High[0] >= ReferenceBOLevel[0] + TicksToBO * TickSize)
					{
						if (ReferenceBar == 0)
						{
							Draw.Square(this, @"LimeSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
							isBOUpSwing4 = true;
							if (Close[0] >= ReferenceBOLevel[0] + TicksToBO * TickSize)
							{
								if (iSwing14.SwingHigh[0] + TicksToBO * TickSize < Close[0])
								{
									if (Close[0] - ReferenceSMA[0] <= CurrentStop)
									{
										isActiveLongPosition = Buy("MarketOrder", Close);
									}
								}
								else
								{
									if (Close[0] - ReferenceSMA[0] <= CurrentStop && iSwing14.SwingHigh[0] - Close[0] > iATR[0] * ClosnessFactor)
									{
										isActiveLongPosition = Buy("MarketOrder", Close);
									}
								}									
							}
						}
						else
						{
							isBOUpSwing4 = true;
						}
					}
					else
					{
						if (MaxHighSwingHigh4 <= ReferenceBOLevel[ReferenceSMABOBar])
						{
							if (MaxHighSwingHigh4 >= ReferenceBOLevel[ReferenceSMABOBar] + TicksToBO * TickSize)
							{							
								isBOUpSwing4 = true;
							}
						}
						else 
						{
							if (MaxHighSwingHigh4 >= ReferenceBOLevel[0] + TicksToBO * TickSize)
							{							
								isBOUpSwing4 = true;
							}
						}
					}
					if (!isBOUpSwing4)
					{
						Draw.Square(this, @"LimeSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] + 3 * TicksToBO * TickSize, Brushes.Lime);
					}
				}
				else
				{
					if (MaxHighSwingHigh4 <= iSwing4.SwingHigh[0])
					{
						isBOUpSwing4 = false;	
					}
					else if (Close[0] >= LastMaxHighSwingHigh4 + TicksToBO * TickSize && ReferenceBar == 0)
					{
						Draw.Dot(this, @"CyanSquare" + CurrentBar, true, 0, LastMaxHighSwingHigh4 + 3 * TicksToBO * TickSize, Brushes.Cyan);
						if (iSwing14.SwingHigh[0] + TicksToBO * TickSize < Close[0])
						{
							if (Close[0] - ReferenceSMA[0] <= CurrentStop)
							{
								isActiveLongPosition = Buy("MarketOrder", Close);
							}
						}
						else
						{
							if (Close[0] - ReferenceSMA[0] <= CurrentStop && iSwing14.SwingHigh[0] - Close[0] > iATR[0] * ClosnessFactor)
							{
								isActiveLongPosition = Buy("MarketOrder", Close);
							}
						}									
					}
				}
				return new Tuple<bool, bool> (isBOUpSwing4, isActiveLongPosition);
			}
			
////		DOWN
			else if (Type == "Down")
			{				
				if (!isBODownSwing4)
				{						
					if (Low[0] <= ReferenceBOLevel[0] - TicksToBO * TickSize)
					{	
						if (ReferenceBar == 0)
						{
							Draw.Square(this, @"RedSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] - 3 * TicksToBO * TickSize, Brushes.Red);
							isBODownSwing4 = true;
							if (Close[0] <= ReferenceBOLevel[0] - TicksToBO * TickSize)
							{	
								if (iSwing14.SwingLow[0] > Close[0])
								{
									if (ReferenceSMA[0] - Close[0] <= CurrentStop)
									{
										isActiveShortPosition = Sell("MarketOrder", Close);
									}
								}
								else
								{
									if (ReferenceSMA[0] - Close[0] <= CurrentStop && Close[0] - iSwing14.SwingLow[0] > iATR[0] * ClosnessFactor)
									{
										isActiveShortPosition = Sell("MarketOrder", Close);
									}
								}
							}
						}
						else
						{
							isBODownSwing4 = true;
						}
					}
					else
					{
						if (MinLowSwingLow4 >= ReferenceBOLevel[ReferenceSMABOBar])
						{
							if (MinLowSwingLow4 <= ReferenceBOLevel[ReferenceSMABOBar] - TicksToBO * TickSize)
							{
								isBODownSwing4 = true;
							}
						}
						else
						{
							if (MinLowSwingLow4 <= ReferenceBOLevel[0] - TicksToBO * TickSize)
							{
								isBODownSwing4 = true;
							}
						}
					}
					if (!isBODownSwing4)
					{
						Draw.Square(this, @"RedSquare" + CurrentBar, true, 0, ReferenceBOLevel[0] - 3 * TicksToBO * TickSize, Brushes.Red);
					}
				}
				else
				{
					if (MinLowSwingLow4 >= iSwing4.SwingLow[0])
					{
						isBODownSwing4 = false;
					}
					else if (Close[0] <= LastMinLowSwingLow4 - TicksToBO * TickSize && ReferenceBar == 0)
					{
						Draw.Dot(this, @"CyanSquare" + CurrentBar, true, 0, LastMinLowSwingLow4 - 3 * TicksToBO * TickSize, Brushes.Indigo);
						if (iSwing14.SwingLow[0] > Close[0])
						{
							if (ReferenceSMA[0] - Close[0] <= CurrentStop)
							{
								isActiveShortPosition = Sell("MarketOrder", Close);
							}
						}
						else
						{
							if (ReferenceSMA[0] - Close[0] <= CurrentStop && Close[0] - iSwing14.SwingLow[0] > iATR[0] * ClosnessFactor)
							{
								isActiveShortPosition = Sell("MarketOrder", Close);
							}
						}
					}
				}
				return new Tuple<bool, bool> (isBODownSwing4, isActiveShortPosition);
			}
			return new Tuple<bool, bool> (isBOUpSwing4, isActiveLongPosition);
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
							if (Close[0] - ReferenceSMA[0] <= CurrentStop)
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
							if (ReferenceSMA[0] - Close[0] <= CurrentStop)
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
				return new Tuple<bool, bool> (isBO, isActiveShortPosition);
			}
			return new Tuple<bool, bool> (isBO, isActiveLongPosition);
		}		
	}
}
