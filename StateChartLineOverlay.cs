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
    
    [ContentProperty(Label="Event Data Source Key", DefaultValuesKey="", GroupKey=nameof(ContentPropertyGroup._GENERAL), DisplayOrder=1)]
    public string dataSourceKey { get; set; } = "";

    [ContentProperty(Label="Line Data Source Key", DefaultValuesKey="", GroupKey=nameof(ContentPropertyGroup._GENERAL), DisplayOrder=2)]
    public string lineDataSourceKey { get; set; } = "";
    
    
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
        
        foreach (var row in groups)
        {
            //  this.Ets.Debug.Trace(this.Ets.ToJson(row));
            var backgroundColor = -Int32.Parse(row.GetString("OeeEventTypeColor", ""));
            
            var StartDateTimeOffset = row.GetString("StartDateTimeOffset", "");
            var EndDateTimeOffset = row.GetString("EndDateTimeOffset", "");
            // generate "running" bar when last event end does not equal start of current event
            if (lastEndTimeOffset != StartDateTimeOffset && lastEndTimeOffset!= "") {
                // only generate if event has ended
                if (!string.IsNullOrEmpty(EndDateTimeOffset) && !string.IsNullOrEmpty(StartDateTimeOffset)) {
                    this.Ets.Debug.Trace("Date strings");
                    this.Ets.Debug.Trace(EndDateTimeOffset);
                    this.Ets.Debug.Trace(StartDateTimeOffset);
                    var sdto = DateTimeOffset.Parse(StartDateTimeOffset);
                    var edto = DateTimeOffset.Parse(EndDateTimeOffset);
                    this.Ets.Debug.TraceObject((sdto - edto));
                    
                    // this.Ets.Debug.TraceObject(datasets.GenerateRunning(StartDateTimeOffset, EndDateTimeOffset));
                    datasets.datasets.Add(datasets.GenerateRunning(StartDateTimeOffset, EndDateTimeOffset));

                    lastEndTimeOffset = EndDateTimeOffset;
                    this.Ets.Debug.Trace("New EDT");
                    this.Ets.Debug.Trace(lastEndTimeOffset);
                }
            }
            if (lastEndTimeOffset == "") {
                // populate start on first event
                chartStartTime = StartDateTimeOffset;
            }
            
            datasets.datasets.Add(new ChartData() 
                {
                label = row.GetString("EventDefinitionName", "NotSet"),
                eventId = row.GetString("EventID", "N/A"),
                groupId = "Group 1",
                type = "horizontalBar",
                xAxisID = "event-line",
                yAxisID = "event",
                data = new List<Object> { row.GetInteger("DurationSeconds", 0) },
                backgroundColor = "#" + (backgroundColor).ToString("X"),
                }
            );
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

        var lineGroups = lineData.Select();
        // new array for speed values
        List<Object> speedArr = new List<Object>(); 
        foreach (var row in groups)
        {
            timestamp = row.GetString("GroupValue", null);
            value = row.GetString("Value", null);
            speedArr.Add(
                new Dictionary<string, Object> { {"x", timestamp}, {"y", value} }
            );
        };
        
        this.Ets.Debug.Trace(this.Ets.ToJson(speedArr));

        var dateTimeStart = DateTimeOffset.Parse(chartStartTime);
        this.Ets.Debug.Trace(dateTimeStart.ToString());
        datasets.datasets.Insert(0, new ChartData() 
            {
                label = "Speed",
                eventId = "Speed",
                groupId = "Speed",
                type = "line",
                xAxisID = "time-line",
                yAxisID = "line",
                data = new List<Object> {
                    new Dictionary<string, Object> { {"x", dateTimeStart.ToString()}, {"y", 20} },
                    new Dictionary<string, Object> { {"x", dateTimeStart.AddMinutes(10).ToString()}, {"y", 200} },
                    new Dictionary<string, Object> { {"x", dateTimeStart.AddMinutes(20).ToString()}, {"y", 300} },
                    new Dictionary<string, Object> { {"x", dateTimeStart.AddMinutes(30).ToString()}, {"y", 250} },
                    new Dictionary<string, Object> { {"x", dateTimeStart.AddMinutes(40).ToString()}, {"y", 100} },
                    new Dictionary<string, Object> { {"x", dateTimeStart.AddMinutes(50).ToString()}, {"y", 200} },
                    new Dictionary<string, Object> { {"x", dateTimeStart.AddMinutes(60).ToString()}, {"y", 300} },
                    new Dictionary<string, Object> { {"x", dateTimeStart.AddMinutes(70).ToString()}, {"y", 250} },
                    new Dictionary<string, Object> { {"x", dateTimeStart.AddMinutes(80).ToString()}, {"y", 100} },
                    new Dictionary<string, Object> { {"x", dateTimeStart.AddMinutes(90).ToString()}, {"y", 200} },
                    new Dictionary<string, Object> { {"x", dateTimeStart.AddMinutes(100).ToString()}, {"y", 300} },
                    new Dictionary<string, Object> { {"x", dateTimeStart.AddMinutes(110).ToString()}, {"y", 250} },
                    new Dictionary<string, Object> { {"x", dateTimeStart.AddMinutes(120).ToString()}, {"y", 100} },
                },
                backgroundColor = "#000000",
                fill = 0,
            }
        );

        
        
        this.Ets.Debug.Trace(this.Ets.ToJson(datasets));

        string chartJsInit = @"
        var testClick = (event) => {
            var firstPoint = chart.getElementAtEvent(event)[0];
            console.log('firstPoint', firstPoint);
            console.log('dataset index', firstPoint._datasetIndex);
            console.log('chart datasets', chart.data.datasets);
            if (firstPoint) {
                // var value = chart.data.datasets[firstPoint._datasetIndex].data[firstPoint._index];
                var data = chart.data.datasets[firstPoint._datasetIndex];
            }
            // console.log('value', value);
            console.log('data', data);
        };
        var ctx = document.getElementById('stateChartLineOverlay').getContext('2d');
        var startDate = new Date();
        
        var chart = new Chart(ctx, {
            // The type of chart we want to create
            type: 'horizontalBar',

            // The data for our dataset
            data: {
                labels: ['Event'],
                datasets: REPLACE_DATASETS
            },

            // Configuration options go here
            options: {
                onClick: testClick,
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
                backgroundColor = "#f0f0f0",
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
        public int fill;
        
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
        }
    }
  }
}
