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
  public partial class CustomTileRepeaterPart : ContentPartBase
  {
    //// Declare properties with ContentProperty Attribute so they are exposed for editing from the Part Editor
    //[ContentProperty(Label="System ID", DefaultValuesKey="SystemID", GroupKey=nameof(ContentPropertyGroup._GENERAL), DisplayOrder=1)]
    //public int SystemID { get; set; } = -1;
    
      [ContentProperty(
          Label="Source Key"
        , GroupKey=nameof(ContentPropertyGroup._GENERAL)
        , DisplayOrder=1
        , ShouldRenderKeyInput=false)]
      public string TargetDateTimeKey { get; set; } = "";

      [ContentProperty(
          Label="Dataset Source Key"
        , GroupKey=nameof(ContentPropertyGroup._GENERAL)
        , DisplayOrder=1
        , ShouldRenderKeyInput=false)]
      public string DatasetKey { get; set; } = "";
      
      // general color
      [ContentProperty(
          Label="Start Color"
        , GroupKey=nameof(ContentPropertyGroup._GENERAL)
        , SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_COLOR)
        , DisplayOrder=1, ControlTypeKey = ControlTypeKey.ColorPalette)]
      public string StartingColorCss { get; set; } = "tscolor-success";

      [ContentProperty(
          Label="Mid Color"
        , GroupKey=nameof(ContentPropertyGroup._GENERAL)
        , SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_COLOR)
        , DisplayOrder=3, ControlTypeKey = ControlTypeKey.ColorPalette)]
      public string MiddleColorCss { get; set; } = "tscolor-warning";

      [ContentProperty(
          Label="End Color"
        , GroupKey=nameof(ContentPropertyGroup._GENERAL)
        , SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_COLOR)
        , DisplayOrder=5, ControlTypeKey = ControlTypeKey.ColorPalette)]
      public string EndingColorCss { get; set; } = "tscolor-alert";

      [ContentProperty(
          Label="Mid Start Value (Seconds)"
        , GroupKey=nameof(ContentPropertyGroup._GENERAL)
        , SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_COLOR)
        , DisplayOrder=2)]
      public int MidStartValueSeconds { get; set; } = -1;

      [ContentProperty(
          Label="Mid End Value (Seconds)"
        , GroupKey=nameof(ContentPropertyGroup._GENERAL)
        , SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_COLOR)
        , DisplayOrder=4)]
      public int MidEndValueSeconds { get; set; } = -1;

      // general text
      [ContentProperty(
          Label="Text"
        , GroupKey=nameof(ContentPropertyGroup._GENERAL)
        , SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_TEXT)
        , DisplayOrder=1
        , ShouldRenderKeyInput=false
        , ControlTypeKey = ControlTypeKey.ProcessExpressionResourceItem)]
      public ResourceStringItem TextEx { get; set; } = "";
      
       // SubEquipment URL
      [ContentProperty(
          Label="Sub-Equipment Screen Key"
        , GroupKey=nameof(ContentPropertyGroup._GENERAL)
        , SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_TEXT)
        , DisplayOrder=1
        , ShouldRenderKeyInput=false
        , ControlTypeKey = ControlTypeKey.ProcessExpressionResourceItem)]
      public ResourceStringItem SubEquipmentURL { get; set; } = "pack_subsys";

      // Operator Dashboard URL
      [ContentProperty(
          Label="Operator Dashboard Screen Key"
        , GroupKey=nameof(ContentPropertyGroup._GENERAL)
        , SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_TEXT)
        , DisplayOrder=1
        , ShouldRenderKeyInput=false
        , ControlTypeKey = ControlTypeKey.ProcessExpressionResourceItem)]
      public ResourceStringItem OperatorURL { get; set; } = "operatordashboard";

       // Background Image URL
      [ContentProperty(
          Label="Background Image URL"
        , GroupKey=nameof(ContentPropertyGroup._GENERAL)
        , SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_APPEARANCE)
        , DisplayOrder=1
        , ShouldRenderKeyInput=false
        , ControlTypeKey = ControlTypeKey.ProcessExpressionResourceItem)]
      public ResourceStringItem BackgroundImageURL { get; set; } = "";
      
       // general color
      [ContentProperty(
          Label="General Color"
        , GroupKey=nameof(ContentPropertyGroup._GENERAL)
        , SubGroupKey=nameof(ContentPropertySubGroup._GENERAL_APPEARANCE)
        , DisplayOrder=1
        , ShouldRenderKeyInput=false
        , ControlTypeKey = ControlTypeKey.ColorPalette)]
      public string GeneralColor { get; set; } = "tscolor-default";
      
      
    /// <summary>
    /// ///////////////////////////////////////////////////
    /// </summary>
    protected DateTimeOffset _targetDateTimeValue;
    protected DataTable _DatasetKey;
    protected override bool ContentPart_Init()
    {
    
        this.
      _targetDateTimeValue = this.Ets.Values.GetAsDateTimeOffset(
          this.TargetDateTimeKey
        , ETS.Core.Constants.NullDateTimeOffset
        );      
      if (_targetDateTimeValue.IsNull()) 
        return this.Ets.Debug.FailContentPage("The TargetDateTimeKey is required");
      
      _DatasetKey = this.Ets.Values.GetAs(this.DatasetKey, (DataTable) null).ThrowIfLoadFailed("SourceDataKey", DatasetKey);
      if (_DatasetKey == null || _DatasetKey.Rows.Count == 0) {}
        //return this.Ets.Debug.FailContentPage("The DatasetKey is required");
      
      // calculate the color based on TargetDateTime
      DateTimeOffset currentDateTimeOffset = this.Ets.SiteNow;
      int durationInSeconds = (_targetDateTimeValue - currentDateTimeOffset).Seconds;
      
      _actualColorCss = this.StartingColorCss;
      
      if(durationInSeconds <= this.MidStartValueSeconds 
        && durationInSeconds > this.MidEndValueSeconds) 
      {
        _actualColorCss = this.MiddleColorCss;
      }
      else if(durationInSeconds < this.MidEndValueSeconds)
      {
        _actualColorCss = this.EndingColorCss;
      }  
      
      if (this.TextEx.IsNullOrWhiteSpace())
      {
        this.TextEx = "{" + this.TargetDateTimeKey + ":timer-down:hms} remaining";
      }
      
     
      //this.Ets.Js.Add(CreateJs());
      this.Ets.Js.Add(CreateTiles());
      
      return true;
    }
    
    protected string _actualColorCss;
    protected override string RootCss()
    {
      var cssList = new List<string>();
      //   cssList.Add("tstile");
      cssList.Add(base.RootCss());
      // height
      // cssList.Add("tstile-h-medium");
      // width
      //  cssList.Add("col-sm-12");
      // color
      cssList.AddIfNotNull(this.CssClassExtra);
      
      var css = new System.Text.StringBuilder();
      bool isFirst = true;
      foreach (var s in cssList)
      {
          isFirst = css.AppendSeperatorIfNecessary(isFirst, " ");
          css.Append(s);
      }  
      
      return css.ToString();
    }
    
    protected IHtmlString _textValue;
    protected IHtmlString _textValueIDs;
    /// ***********************************************************
    protected override bool DataBindProcessExpressionToChildControls()
    {
      if (!base.DataBindProcessExpressionToChildControls()) return false;
      
      _textValue = this.Ets.ProcessExpressionLive(this.TextEx, null, this.ID, "TextEx");
      
      string _STRtextValueIDs = "";
      foreach (DataRow r in _DatasetKey.Rows)
      {
        _STRtextValueIDs = _STRtextValueIDs+r["LineName"].ToString()+"; ";
      }
      _textValueIDs = this.Ets.ProcessExpressionLive(_STRtextValueIDs, null, this.ID, "_textValueIDs");
      
      return true;
    }
    
    private string CreateTiles()
    {
      if(!(_DatasetKey.Rows.Count>0)) { return "";}
      
      string partID = this.Ets.ProcessExpressionLive(this.TextEx, null, this.ID, "TextEx").ToString();
      string html_string ="";
      foreach (DataRow r in _DatasetKey.Rows)
      {
        string line = r["LineName"].ToString();
        string job =  r["JobName"].ToString();
        string prod = r["ProductName"].ToString();
        string urlLink = r["MainScreenURL"].ToString()+"$"+r["InstanceURL"].ToString();
        string topBorderColor = "#696969";//"#D9534F";
        string iconName = "fa ";
        switch(r["Status"]) 
        {
          case(0): iconName+="fa-circle-o"; break;
          case(1): iconName+="fa-dot-circle-o"; break;
          case(2): iconName+="fa-play-circle-o"; break;
          case(4): iconName+="fa-arrow-circle-o-up"; break;
          case(8): iconName+="fa-circle-o"; break;
          default: iconName+="fa-circle-o"; break;
        }
          
        // <div><button type=""button"" style=""width:260px; margin-left:7px; border: none;"" onclick={7}>OPERATOR</button></div> 

        html_string = html_string + @"
          <div> 
            <div>
                <button type=""button"" style=""width:260px; margin-left:7px; border: none;"" onclick={7}>OPERATOR</button>
            </div> 
            <div id=""{8}"" class=""tstile tscontentpart-fixed tstile-h-xlarge tstile-xs-w-xlarge {4} tstile-band-top-small""> 
              <div class=""tstile-body"" style=""border-top-color:{5};""> 
                <div class=""tstile-bg"" style=""background-image:url({3});""></div>
                <div class=""tstile-upper"">
                  <div class=""tstile-text-medium"">{0}</div>
                  <div class=""tstile-text-xsmall"">Order: {1}</div>
                  <div class=""tstile-text-small"">{2}</div>
                </div>
                <div class=""tstile-lower text-right"" style=""color:black""> 
                  <div>
                      <button type=""button"" style="" width:231px""  onclick={6}>LINE</button>
                  </div> 
        ".FormatWith(
          line, 
          job, 
          prod, 
          BackgroundImageURL, 
          GeneralColor, 
          topBorderColor, 
          HttpUtility.JavaScriptStringEncode("NavTo('{0}')".FormatWith(urlLink)), 
          HttpUtility.JavaScriptStringEncode("NavTo('{0}')".FormatWith(urlLink+"/"+OperatorURL)), 
          partID
        );
      
        //START 2nd FORLOOP FOR SS BUTTONS
        DataTable _DatasetSubSys = this.Ets.Api.Util.Db.GetDataTable(@"DECLARE @LineSystemID INT = {0};
                                                                      SELECT s1.Name, s1.[ID], s1.[AltName], s2.ID [SSID] FROM tSystem s1
                                                                      LEFT JOIN (SELECT * FROM tSystem WHERE ParentSystemID = @LineSystemID) s2 ON s2.LinkedSystemID = s1.ID
                                                                      LEFT JOIN [dbo].[viewCustomPropertySystem] vcps ON vcps.ID = s1.ID
                                                                      WHERE (s2.ParentSystemID = @LineSystemID)
                                                                      AND (vcps.[SYSDEF.ShowInTile] = 1)
                                                                      ORDER BY s1.DisplayOrder ASC".FormatWith(  r["ID"].ToString().ToSql() ) ).Return ;
        foreach (DataRow r2 in _DatasetSubSys.Rows)
        {
            html_string = html_string + @"
            <button type=""button"" style=""width:55px""  onclick={1}>{0}</button> ".FormatWith( r2["AltName"].ToString(), HttpUtility.JavaScriptStringEncode("NavTo('{0}')".FormatWith(urlLink+"/"+SubEquipmentURL+"/?SubSystemID="+r2["SSID"]))  );
        } //END 2nd FORLOOP
            
        html_string = html_string+ @"      
                </div>
                <i class=""{0}""style=""position:relative;float:right;overflow:hidden;font-size:32px""></i>
              </div>
            </div>  
          </div>
        ".FormatWith( iconName.ToString() ) ;  
   
      }
      //END OUTER FOR-LOOP
      
      html_string = html_string.Replace("\n", String.Empty);
      html_string = html_string.Replace("\r", String.Empty);
      html_string = html_string.Replace("\t", String.Empty);
     // html_string = "zzzz";
      
      
      string js = @"
      function "+this.ClientID+@"_InsertTile() 
      {
        var index = '';
        var item = 'b';
        document.getElementById('TileSetHere_"+this.ClientID+"').innerHTML = ' "+html_string+@"  <br></br>'; 
      }
      ets.ready(function(page) {
        "+this.ClientID+@"_InsertTile();
      });
      ";
      return js;
    }
    
    private string CreateJs()
    {      
      string js = @"
      function "+this.ClientID+@"_UpdateTileState () {
        var $part = $('#"+this.ClientID+@"');
        
        var tdtString = $part.attr('data-targetdatetime');
        
        // calculate duration
        var tdt = ets.dateTime.getDateObjectFrom(tdtString);
        var now = ets.dateTime.getNow();
        var momentNow = moment(now, now.etsOffset);
        var durationInSeconds = (tdt - momentNow)/1000;
        
        // remove any color(s) from $part
        $part.removeClass (function (index, className) {
          return (className.match (/(^|\s)tscolor-\S+/g) || []).join(' ');
        });

        var startColor = $part.attr('data-start-color');
        var midColor = $part.attr('data-midpoint-color');
        var endColor = $part.attr('data-endpoint-color');

        var midStartValueSeconds = 
            ets.util.tryIntParse($part.attr('data-mid-start'),-1);
        var midEndValueSeconds = 
            ets.util.tryIntParse($part.attr('data-mid-end'),-1);

        // figure out what color to apply to $part
        if(durationInSeconds > midStartValueSeconds) {
          $part.addClass(startColor);
        }
        else if(durationInSeconds <= midStartValueSeconds 
               && durationInSeconds > midEndValueSeconds) {
          $part.addClass(midColor);
        }
        else if(durationInSeconds < midEndValueSeconds) {
          $part.addClass(endColor);
        }  
        
        ets.util.asyncWait(1000).then(function() {
         "+this.ClientID+@"_UpdateTileState();
       });
      }
      ets.ready(function(page) {
        "+this.ClientID+@"_UpdateTileState();
      });";
      
      return js;
    }

  }
}
