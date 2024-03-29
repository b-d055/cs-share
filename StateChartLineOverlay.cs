using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.ModelBinding;
using System.Web.UI.DataVisualization.Charting;
using ETS.Core.Api;
using ETS.Core.Api.Models;
using ETS.Core.Api.Models.Data;
using ETS.Core.Enums;
using ETS.Core.Extensions;
using ETS.Core.Scripting;
using ETS.Core.Services.Resource;
using ETS.Ts.Core.ContentParts;
using ETS.Ts.Core.Enums;
using ETS.Ts.Core.Scripting;

namespace ETS.Ts.Content
{
  /// ***********************************************************
  public partial class StateChartLineOverlayPart : ContentPartBase
  {
    //// Declare properties with ContentProperty Attribute so they are exposed for editing from the Part Editor
    //[ContentProperty(Label="System ID", DefaultValuesKey="SystemID", GroupKey=nameof(ContentPropertyGroup._GENERAL), DisplayOrder=1)]
    //public int SystemID { get; set; } = -1;
    
    [ContentProperty(
        Label="Event Data Source Key", 
        DefaultValuesKey="", 
        GroupKey=nameof(ContentPropertyGroup._GENERAL), 
        DisplayOrder=1)]
    public string dataSourceKey { get; set; } = "";

    [ContentProperty(
        Label="Line Data Source Key", 
        DefaultValuesKey="", 
        GroupKey=nameof(ContentPropertyGroup._GENERAL), 
        DisplayOrder=2)]
    public string lineDataSourceKey { get; set; } = "";

    [ContentProperty(
        Label="Ignore Duplicate Line Readings", 
        DefaultValuesKey="", 
        GroupKey=nameof(ContentPropertyGroup._GENERAL), 
        DisplayOrder=3)]
    public int ignoreDuplicateLineReadingsKey { get; set; } = 0;

    [ContentProperty(
        Label="Line Area Fill", 
        DefaultValuesKey="", 
        GroupKey=nameof(ContentPropertyGroup._GENERAL), 
        DisplayOrder=4)]
    public string lineFillKey { get; set; } = "0";

    [ContentProperty(
        Label="Event Label", 
        DefaultValuesKey="", 
        GroupKey=nameof(ContentPropertyGroup._GENERAL), 
        SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_FIELDS),
        DisplayOrder=1)]
    public string eventLabelKey { get; set; } = "";

    [ContentProperty(
        Label="Event ID", 
        DefaultValuesKey="", 
        GroupKey=nameof(ContentPropertyGroup._GENERAL), 
        SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_FIELDS),
        DisplayOrder=2)]
    public string eventIdKey { get; set; } = "";

    [ContentProperty(
        Label="Event Group ID", 
        DefaultValuesKey="", 
        GroupKey=nameof(ContentPropertyGroup._GENERAL), 
        SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_FIELDS), 
        DisplayOrder=3)]
    public string eventGroupIdKey { get; set; } = "";

    [ContentProperty(
        Label="Event Group Name", 
        DefaultValuesKey="", 
        GroupKey=nameof(ContentPropertyGroup._GENERAL), 
        SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_FIELDS), 
        DisplayOrder=4)]
    public string eventGroupNameKey { get; set; } = "";

    [ContentProperty(
        Label="Event Group Color", 
        DefaultValuesKey="", 
        GroupKey=nameof(ContentPropertyGroup._GENERAL), 
        SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_FIELDS),  
        DisplayOrder=5)]
    public string eventGroupColorKey { get; set; } = "";
    
    
    /// ***********************************************************
    protected override bool ContentPart_Init()
    {
        if (!this.Visible) return true;

        this.Ets.Debug.Trace("StateChartLineOverlay Init");

        var ShiftStartDateTimeOffset = this.Ets.Values.GetAsObject("Data.Shift.Selected.StartDateTimeOffset", "");
        var ShiftEndDateTimeOffset = this.Ets.Values.GetAsObject("Data.Shift.Selected.EndDateTimeOffset", "");

        this.Ets.Debug.TraceObject(ShiftStartDateTimeOffset);
        this.Ets.Debug.TraceObject(ShiftEndDateTimeOffset);
        
        DataTable data = (DataTable) this.Ets.Values.GetAsObject(dataSourceKey, null);
        DataTable lineData = (DataTable) this.Ets.Values.GetAsObject(lineDataSourceKey, null);
        
        // this.Ets.Debug.TraceObject(data);
        // this.Ets.Debug.TraceObject(lineData);

        if (data == null || lineData == null) {
            // no content, do not render 
            return true;
        }
        
        EventChartDatasets datasets = new EventChartDatasets();
        
        var groups = data.Select();
        var lastEndTimeOffset = "";

        // user first start time as start time for graph
        var chartStartTime = "";

        if (groups.Length > 0) {
            foreach (var row in groups)
            {
                //  this.Ets.Debug.Trace(this.Ets.ToJson(row));
                // var backgroundColor = -Int32.Parse(row.GetString("OeeEventTypeColor", ""));
                var eventGroupColor = row.GetString(eventGroupColorKey, null);
                this.Ets.Debug.Trace("Event Definition Type Color");
                var backgroundColor = eventGroupColor.AsColor().ToHexDisplay();
                this.Ets.Debug.TraceObject(backgroundColor);
                
                var StartDateTimeOffset = row.GetString("StartDateTimeOffset", "");
                var EndDateTimeOffset = row.GetString("EndDateTimeOffset", "");
                // generate "running" bar at beginning of shift if first event does not match shift start
                if (ShiftStartDateTimeOffset.ToString() != StartDateTimeOffset && lastEndTimeOffset== "") {
                    datasets.datasets.Add(datasets.GenerateRunning(ShiftStartDateTimeOffset.ToString(), StartDateTimeOffset));
                }
                // generate "running" bar when last event end does not equal start of current event
                if (lastEndTimeOffset != StartDateTimeOffset && lastEndTimeOffset!= "") {
                    // only generate if event has ended
                    if (!string.IsNullOrEmpty(lastEndTimeOffset) && !string.IsNullOrEmpty(StartDateTimeOffset)) {
                        this.Ets.Debug.Trace("Date strings");
                        this.Ets.Debug.Trace(lastEndTimeOffset);
                        this.Ets.Debug.Trace(StartDateTimeOffset);
                        this.Ets.Debug.Trace(EndDateTimeOffset);
                        // var sdto = DateTimeOffset.Parse(StartDateTimeOffset);
                        // var edto = DateTimeOffset.Parse(EndDateTimeOffset);
                        // this.Ets.Debug.TraceObject((sdto - edto));
                        
                        datasets.datasets.Add(datasets.GenerateRunning(lastEndTimeOffset, StartDateTimeOffset));
                    }
                }
                if (lastEndTimeOffset == "") {
                    // populate start on first event
                    chartStartTime = StartDateTimeOffset;
                }

                lastEndTimeOffset = EndDateTimeOffset;
                this.Ets.Debug.Trace("New EDT");
                this.Ets.Debug.Trace(lastEndTimeOffset);

                var showForAcknowledge = row.GetInteger("ShowForAcknowledge", 1);
                var isActive = row.GetBoolean("IsActive", false);
                var isClickable = true;
                // do not make active events or events not shown for acknowledge clickable
                if (isActive == true || showForAcknowledge == 0) {
                    isClickable = false;
                }
                
                datasets.datasets.Add(new ChartData() 
                    {
                    label = row.GetString(eventLabelKey, "N/A"),
                    eventId = row.GetString(eventIdKey, "N/A"),
                    groupId = row.GetString(eventGroupIdKey, "N/A"),
                    type = "horizontalBar",
                    xAxisID = "event-line",
                    yAxisID = "event",
                    data = new List<Object> { row.GetInteger("DurationSeconds", 0) },
                    backgroundColor = backgroundColor,
                    notes = row.GetString("Notes", ""),
                    subCategory = row.GetString("RcaDescription", ""),
                    machine = row.GetString("RcaSystemId", ""),
                    isClickable = isClickable,
                    }
                );
            }
            this.Ets.Debug.Trace("Generate last running details");
            this.Ets.Debug.Trace(lastEndTimeOffset);
            this.Ets.Debug.Trace(ShiftEndDateTimeOffset.ToString());
            if (lastEndTimeOffset != ShiftEndDateTimeOffset.ToString() && lastEndTimeOffset!= "") {
                // generate running block for last event if last event did not go to end of shift
                datasets.datasets.Add(datasets.GenerateRunning(lastEndTimeOffset, ShiftEndDateTimeOffset.ToString()));
            }
        }

        // capture total seconds to make sure line and bar charts line up
        // user ShiftEnd and Start times for start and end of chart axes
        this.Ets.Debug.Trace("Parsing sdt edt");
        // this.Ets.Debug.Trace(chartStartTime);
        // this.Ets.Debug.Trace(lastEndTimeOffset);
        var sdt = DateTimeOffset.Parse(ShiftStartDateTimeOffset.ToString());
        var edt = DateTimeOffset.Parse(ShiftEndDateTimeOffset.ToString());

        this.Ets.Debug.Trace("done parsing");
        var timespan = edt - sdt;
        var totalChartSeconds = timespan.TotalSeconds;
        this.Ets.Debug.Trace(totalChartSeconds.ToString());

        // var lineGroups = lineData.Select();
        // new array for speed values
        List<Object> speedArr = new List<Object>(); 
        double lastCount = -1.0;
        foreach (DataRow row in lineData.Rows)
        {
            var timestamp = row["GroupValue"];
            // only get difference between this value and last value
            double currCount = Convert.ToDouble(row["Value"]);
            // set to curr count if first loop
            if (lastCount == -1.0) {
                lastCount = currCount;
            }
            double currValue = (currCount - lastCount);
            if (currValue < 0) {
                currValue = 0;
            };
            // only add if greater than 0 to avoid cluttering
            if (ignoreDuplicateLineReadingsKey == 0 || currValue > 0) {
                speedArr.Add(
                    new Dictionary<string, Object> { {"x", timestamp}, {"y", currValue} }
                );
            }
            lastCount = currCount;
        };
        
        this.Ets.Debug.Trace(this.Ets.ToJson(speedArr));
        if (chartStartTime != "") {
            var dateTimeStart = DateTimeOffset.Parse(chartStartTime);
            this.Ets.Debug.Trace(dateTimeStart.ToString());
        } else {
            this.Ets.Debug.Trace("No dateTimeStart (no events)");
        }
       
        datasets.datasets.Insert(0, new ChartData() 
            {
                label = "Speed",
                eventId = "Speed",
                groupId = "Speed",
                type = "line",
                xAxisID = "time-line",
                yAxisID = "line",
                data = speedArr,
                backgroundColor = "#000000",
                borderColor = "rgba(0,0,0,0.8)",
                fill = lineFillKey,
            }
        );

        
        
        this.Ets.Debug.Trace(this.Ets.ToJson(datasets));
        
        string chartJsInit = @"
        const openEvent = function(event) {
            // get existing base URL
            var currUrl = window.location;
            var origin = currUrl.origin;
            var pathname = currUrl.pathname;
            pathname = pathname.substring(0, pathname.lastIndexOf('/') + 1);
            var firstPoint = chart.getElementAtEvent(event)[0];
            console.log('firstPoint', firstPoint);
            if (firstPoint && firstPoint._datasetIndex) {
                // console.log('dataset index', firstPoint._datasetIndex);
                // console.log('chart datasets', chart.data.datasets);
                if (firstPoint) {
                    
                    var dataset = chart.data.datasets[firstPoint._datasetIndex];
                    // navigate when click on event, and event is not active or not shown for ack
                    if (dataset.yAxisID === 'event' && dataset.eventId !== 'Running' && dataset.isClickable) {
                        window.location.href = origin + pathname + '_EventEdit?EventID=' + dataset.eventId;
                    }
                }
            } else {
                console.log('not a dataset');
            }
        };
        var ctx = document.getElementById('stateChartLineOverlay').getContext('2d');
        var startDate = new Date();
        
        var chart = new Chart(ctx, {
            // The type of chart we want to create
            type: 'horizontalBar',
            // Placeholder for datasets
            data: {
                labels: ['Event'],
                datasets: REPLACE_DATASETS
            },
            // Configuration options
            options: {
                // turn off animation
                animation: {
                    duration: 0
                },
                // custom click option
                onClick: openEvent,
                // new legend to group events together
                legend: {
                    labels: {
                        generateLabels: function(chart) {
                            var data = chart.data;
                            // console.log('data', data);
                            if (data.labels.length && data.datasets.length) {
                                let eventTypeLabels = {};
                                data.datasets.map(function(dataset, i) {
                                    if (dataset.yAxisID === 'event') {
                                        if (!eventTypeLabels[dataset.groupId]) {
                                            eventTypeLabels[dataset.groupId] = {
                                                text: dataset.groupId,
                                                fillStyle: dataset.backgroundColor,
                                                index: i
                                            }
                                        }
                                    }
                                });
                                // console.log(eventTypeLabels);
                                return Object.keys(eventTypeLabels).map(function(eventType) {
                                    return eventTypeLabels[eventType];
                                })
                            }
                            return [];
                        }
                    }
                },
                // custom tooltips for speed and events
                tooltips: {
                    callbacks: {
                        title: function(tooltipItems, data) {
                            // add eventId to 'Event' title, use date for speed title
                            // console.log('tooltipitems', tooltipItems);
                            var dataset = data.datasets[tooltipItems[0].datasetIndex];
                            if (dataset.yAxisID === 'event') {
                                return tooltipItems[0].yLabel + ' ' + dataset.eventId;
                            } else {
                                return tooltipItems[0].xLabel;
                            }
                        },
                        afterLabel: function(tooltipItem, data) {
                            // only show custom 'seconds' tooltip for events
                            var dataset = data.datasets[tooltipItem.datasetIndex];
                            if (dataset.yAxisID === 'event') {
                                return 'seconds';
                            } else {
                                return '';
                            }
                        },
                        label: function(tooltipItem, data) {
                            // only yLabel for speed, xLabel for events
                            var dataset = data.datasets[tooltipItem.datasetIndex];
                            if (dataset.yAxisID === 'line') {
                                return dataset.label + ': ' + tooltipItem.yLabel;
                            } else {
                                return dataset.label + ': ' + tooltipItem.xLabel;
                            }
                        },
                        footer: function(tooltipItems, data) {
                            // add notes when non-null
                            var dataset = data.datasets[tooltipItems[0].datasetIndex];
                            var footer = dataset.groupId;
                            if (dataset.subCategory) {
                                footer = footer + ' - ' + dataset.subCategory;
                            };
                            if (dataset.machine) {
                                footer = footer + ' - ' + dataset.machine;
                            };
                            if (dataset.notes) {
                                footer = footer +  ' - ' + dataset.notes;
                            };
                            return footer
                        },
                    }
                },
                elements: {
                    line: {
                        tension: 0 // disables bezier curves
                    }
                },
                // separate scales for events and speed
                scales: {
                    xAxes: [
                        {
                            id: 'time-line',
                            type: 'time',
                            displayFormats: {
                                'millisecond': 'MMM DD',
                                'second': 'MMM DD',
                                'minute': 'MMM DD',
                                'hour': 'MMM DD',
                                'day': 'MMM DD',
                                'week': 'MMM DD',
                                'month': 'MMM DD',
                                'quarter': 'MMM DD',
                                'year': 'MMM DD',
                            },
                            position: 'bottom',
                            time: {
                                min: REPLACE_DATE_MIN,
                                max: REPLACE_DATE_MAX,
                            }
                        },
                        {
                            id: 'event-line',
                            type: 'linear',
                            position: 'top',
                            stacked: true,
                            ticks: {
                                min: 0,
                                max: REPLACE_EVENT_MAX,
                            }
                        }
                    ],
                    yAxes: [
                        {
                            id: 'event',
                            stacked: true,
                            position: 'right',
                            display: false,
                            type: 'category',
                            categoryPercentage: 1,
                            barPercentage: 1,
                            gridLines: {
                                offsetGridLines: !0
                            }
                        },
                        {
                            id: 'line',
                            ticks: {
                                min: 0,
                            }
                        }
                    ],
                }
            }
        });
        "
        .Replace("REPLACE_DATASETS", this.Ets.ToJson(datasets.datasets))
        .Replace("REPLACE_DATE_MIN", this.Ets.ToJson(ShiftStartDateTimeOffset))
        .Replace("REPLACE_DATE_MAX", this.Ets.ToJson(ShiftEndDateTimeOffset))
        .Replace("REPLACE_EVENT_MAX", this.Ets.ToJson(totalChartSeconds));
        
        // Add code to ets.readyAll – Code will run on initial load and on every ajax refresh
        this.Ets.Debug.Trace(chartJsInit);
        
        this.Ets.Js.AddBackBoneReadyAllJavascript(chartJsInit);
        
        return true;
    }

    /// ***********************************************************
    protected override bool DataBindProcessExpressionToChildControls()
    {
      if (!base.DataBindProcessExpressionToChildControls()) return false;
      return true;
    }

    public class EventChartDatasets
    {
        public List<ChartData> datasets;
        
        public EventChartDatasets()
        {
        this.datasets = new List<ChartData>();
        }
    
        public ChartData GenerateRunning(string startDateTime, string endDateTime)
        {
            var sdt = DateTimeOffset.Parse(startDateTime);
            var edt = DateTimeOffset.Parse(endDateTime);
            var timespan = edt - sdt;
            var totalSeconds = timespan.TotalSeconds;
            return new ChartData() {
                label = "Running",
                eventId = "Running",
                groupId = "Running",
                type = "horizontalBar",
                xAxisID = "event-line",
                yAxisID = "event",
                data = new List<Object> { totalSeconds },
                backgroundColor = "#6cc14c",
            };
        }
    }
    
    public class ChartData
    {
        public string label;
        public string eventId;
        public string groupId;
        public string type;
        public string xAxisID;
        public string yAxisID;
        public List<Object> data;
        public string backgroundColor;
        public string borderColor;
        public string fill;
        public string notes;
        public string subCategory;
        public string machine;
        public bool isClickable;
        
        public ChartData()
        {
        this.label = "";
        this.eventId = "";
        this.groupId = "";
        this.type = "";
        this.xAxisID = "";
        this.yAxisID = "";
        this.data = new List<Object> {""};
        this.backgroundColor = "";
        this.borderColor = "";
        this.notes = "";
        this.subCategory = "";
        this.machine = "";
        this.isClickable = false;
        }
    }
  }
}
