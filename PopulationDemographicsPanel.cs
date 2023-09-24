using ColossalFramework.UI;
using ColossalFramework.Globalization;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PopulationDemographics
{
    /// <summary>
    /// a panel to display demographics on the screen
    /// </summary>
    public class PopulationDemographicsPanel : UIPanel
    {
        // age constants
        private const int MaxGameAge = 400;                     // obtained from Citizen.Age
        private const float RealAgePerGameAge = 1f / 3.5f;      // obtained from District.GetAverageLifespan
        private const int MaxRealAge = (int)(MaxGameAge * RealAgePerGameAge);

        /// <summary>
        /// attributes for a row or column selection
        /// </summary>
        private class SelectionAttributes
        {
            public string selectionText;
            public string[] headingTexts;
            public Color32[] amountBarColors;   // only rows (not columns) have amount bars

            public SelectionAttributes(string selectionText, string[] headingTexts, Color32[] amountBarColors)
            {
                this.selectionText   = selectionText;
                this.headingTexts    = headingTexts;
                this.amountBarColors = amountBarColors;
            }
        }

        // row selections
        public enum RowSelection
        {
            Age,
            AgeGroup,
            Education,
            Employment,
            Gender,
            Happiness,
            Health,
            Location,
            Residential,
            Student,
            Wealth,
            WellBeing
        }

        // attributes for each row selection
        private class RowSelectionAttributes : Dictionary<RowSelection, SelectionAttributes> { }
        private RowSelectionAttributes _rowSelectionAttributes;

        // maximum number of rows is the number of Ages which goes from 0 to MaxRealAge inclusive
        private const int MaxRows = MaxRealAge + 1;

        // column selections
        public enum ColumnSelection
        {
            None,
            AgeGroup,
            Education,
            Employment,
            Gender,
            Happiness,
            Health,
            Location,
            Residential,
            Student,
            Wealth,
            WellBeing
        }

        // attributes for each column selection
        private class ColumnSelectionAttributes : Dictionary<ColumnSelection, SelectionAttributes> { }
        private ColumnSelectionAttributes _columnSelectionAttributes;

        // maximum number of columns is the number of heading texts for Health which has the most headings
        private const int MaxColumns = 6;

        // text colors
        private static readonly Color32 TextColorNormal = new Color32(185, 221, 254, 255);
        private const float LockedColorMultiplier = 0.5f;
        private static readonly Color32 TextColorLocked = new Color32((byte)(TextColorNormal.r * LockedColorMultiplier), (byte)(TextColorNormal.g * LockedColorMultiplier), (byte)(TextColorNormal.b * LockedColorMultiplier), 255);
        private const float LineColorMultiplier = 0.8f;
        private static readonly Color32 LineColor = new Color32((byte)(TextColorNormal.r * LineColorMultiplier), (byte)(TextColorNormal.g * LineColorMultiplier), (byte)(TextColorNormal.b * LineColorMultiplier), 255);

        // text scales
        private const float TextScaleHeading = 0.625f;
        private const float TextScale = 0.75f;
        private const float TextScaleAge = 0.625f;


        // UI widths
        private const float PaddingWidth = 10f;             // padding around left and right edges of panel and between row/column selections and data rows
        private const float SelectionWidth = 90f;           // width of row/column selections
        private const float DescriptionWidth = 100f;        // width of description label
        private const float AmountWidth = 67f;              // width of each amount label (just large enough to hold 7 digits with grouping symbols)
        private const float AmountSpacing = 4f;             // spacing between amounts
        private const float AmountSpacingAfterTotal = 16f;  // spacing between total amount and moving in amount
        private const float ScrollbarWidth = 16f;           // width of scroll bar
        private const float DataWidth =                     // width of data
            DescriptionWidth +                                  // data description
            MaxColumns * AmountSpacing +                        // spacing before each amount
            MaxColumns * AmountWidth +                          // amounts
            AmountSpacing +                                     // spacing before total
            AmountWidth +                                       // total
            AmountSpacingAfterTotal +                           // spacing after total
            AmountWidth +                                       // moving in
            AmountSpacing +                                     // spacing after moving in
            AmountWidth +                                       // deceased
            AmountSpacing +                                     // spacing after deceased
            ScrollbarWidth;                                     // scroll bar
        private const float PanelTotalWidth =               // total width of the demographics panel
            PaddingWidth +                                      // padding between panel left edge and row/column selections
            SelectionWidth +                                    // row/column selections
            PaddingWidth +                                      // padding between row/column selections and data rows
            DataWidth +                                         // width of data rows
            PaddingWidth;                                       // padding between data rows and panel right edge

        // UI heights
        private const float TitleBarHeight = 40f;           // height of title bar in MenuPanel2 sprite
        private const float PaddingTop = 5f;                // padding between title bar and district drop down
        private const float PaddingHeight = 10f;            // padding around bottom edge of panel and vertical space between UI components
        private const float DistrictHeight = 45f;           // height of district drop down
        private const float DistrictItemHeight = 17f;       // height of items in district drop down list
        private const float HeadingTop =                    // top of heading panel (and row selection label)
            TitleBarHeight +                                    // title bar
            PaddingTop +                                        // spacing after title bar
            DistrictHeight +                                    // district drop down
            PaddingHeight;                                      // after district drop down
        private const float TextHeight = 15f;               // height of data row
        private const float TextHeightAge = 10f;            // height of data row for Age
        private const float SpaceAfterTotalRow = 12f;       // space between total row and moving in row
        private const float HeightOfTotals =                // height of totals section
            4f +                                                // lines before total row
            TextHeight +                                        // total row
            SpaceAfterTotalRow +                                // space after totals
            TextHeight +                                        // moving in row
            TextHeight;                                         // deceased row
        private const float SpaceAfterTotalsSection = 10f;  // space between totals section and legend section
        private const float HeightOfLegend = TextHeight;    // height of legend section
        private float _panelHeightNotAge;                   // panel height when Age is not selected
        private const float PanelHeightForAge = 1000f;      // panel height when Age is selected

        /// <summary>
        /// UI elements for one row of data
        /// the UI components for one data row are placed on this panel, each data row on its own panel
        /// </summary>
        private class DataRowUI : UIPanel
        {
            public UILabel description;
            public UISprite amountBar;
            public UILabel[] amount = new UILabel[MaxColumns];
            public UILabel total;
            public UILabel movingIn;
            public UILabel deceased;
        }

        /// <summary>
        /// UI elements for one row of lines
        /// </summary>
        private class LinesRowUI
        {
            // no line for description/amount bar
            // lines for: amounts, total, moving in, and deceased
            public UISprite[] amount = new UISprite[MaxColumns];
            public UISprite total;
            public UISprite movingIn;
            public UISprite deceased;
        }

        // UI elements that get adjusted based on user selections
        private DataRowUI _heading;
        private LinesRowUI _headingLines;
        private UIPanel _dataPanel;
        private UIScrollablePanel _dataScrollablePanel;
        private UIPanel _dataRowsPanel;
        private DataRowUI[] _dataRows;
        private LinesRowUI _totalLines;
        private DataRowUI _totalRow;
        private DataRowUI _movingInRow;
        private DataRowUI _deceasedRow;
        private UILabel _legendLowValue;
        private UILabel _legendHighValue;

        // UI elements for count/percent buttons
        private UIPanel _countPanel;
        private UIPanel _percentPanel;
        private UISprite _countCheckBox;
        private UISprite _percentCheckBox;

        // UI elements for opacity
        public const float DefaultOpacity = 1f;
        private UISlider _opacitySlider;
        private UILabel _opacityValueLabel;


        // here is the hierarchy of UI elements:
        //
        //  PopulationDemographicsPanel
        //      population icon
        //      title label
        //      close button
        //      district dropdown
        //      opacity label and slider
        //      row selection label and listbox
        //      column selection label and listbox
        //      heading panel
        //          DataRowUI for heading row
        //          LinesRowUI below headings
        //      data panel
        //          data scrollable panel
        //              data rows panel
        //                  DataRowUI's for MaxRows data rows
        //          vertical scroll bar for scrollable panel
        //              scroll bar track
        //                  scroll bar thumb
        //          total panel
        //              LinesRowUI above total row
        //              DataRowUI for total row
        //              DataRowUI for moving in row
        //              DataRowUI for deceased row
        //              display option panel for count
        //                  count checkbox sprite
        //                  count label
        //              display option panel for percent
        //                  percent checkbox sprite
        //                  percent label
        //          legend panel
        //              low value label
        //              color gradient
        //              high value label


        // employment status
        private enum EmploymentStatus
        {
            Student,
            Employed,
            Unemployed
        }

        /// <summary>
        /// the demographic data for one citizen
        /// </summary>
        private class CitizenDemographic
        {
            public uint                 citizenID;
            public byte                 districtID;
            public bool                 deceased;
            public bool                 movingIn;
            public int                  age;            // real age, not game age
            public Citizen.AgeGroup     ageGroup;
            public Citizen.Education    education;
            public EmploymentStatus     employment;
            public Citizen.Gender       gender;
            public Citizen.Happiness    happiness;
            public Citizen.Health       health;
            public Citizen.Location     location;       // Hotel location is not used because only tourists, not citizens, are guests at hotels
            public ItemClass.Level      residential;    // None (i.e. -1) should never happen because Levels 1-5 (i.e. 0-4) are level of citizen's home building
            public ItemClass.Level      student;        // None (i.e. -1) = not a student, Levels 1-3 (i.e. 0-2) = Elementary, High School, University
            public Citizen.Wealth       wealth;
            public Citizen.Wellbeing    wellbeing;
        }

        /// <summary>
        /// the demographic data for a collection of citizens
        /// </summary>
        private class CitizenDemographics : List<CitizenDemographic> { }

        // the citizen demographics buffers
        // temp buffer gets populated in simulation thread
        // final buffer gets updated periodically from temp buffer in simulation thread and is used in UI thread to display the demographics
        private CitizenDemographics _tempCitizens;
        private CitizenDemographics _finalCitizens;

        /// <summary>
        /// the demographic data for one building
        /// </summary>
        private class BuildingDemographic
        {
            public byte  districtID;
            public int   citizenCount;
            public bool  isValid;
            public float avgAge;        // real age, not game age
            public float avgEducation;
            public int   unemployedCount;
            public int   jobEligibleCount;
            public float avgUnemployed; // unemployment rate
            public float avgGender;
            public float avgHappiness;
            public float avgHealth;
            public int   atHomeCount;
            public float avgAtHome;     // Location
            public int   residential;
            public int   studentCount;
            public float avgStudent;
            public float avgWealth;
            public float avgWellbeing;
        }

        // define a min and max for each building average
        private int   _minCitizens;
        private int   _maxCitizens;
        private float _minAge;
        private float _maxAge;
        private float _minEducation;
        private float _maxEducation;
        private float _minUnemployed;
        private float _maxUnemployed;
        private float _minGender;
        private float _maxGender;
        private float _minHappiness;
        private float _maxHappiness;
        private float _minHealth;
        private float _maxHealth;
        private float _minAtHome;
        private float _maxAtHome;
        private int   _minResidential;
        private int   _maxResidential;
        private float _minStudent;
        private float _maxStudent;
        private float _minWealth;
        private float _maxWealth;
        private float _minWellBeing;
        private float _maxWellBeing;

        // the building demographic buffers, one for each possible building
        // temp buffer gets populated in simulation thread
        // final buffer gets updated periodically from temp buffer in simulation thread and is used by UI thread to display the demographics
        private BuildingDemographic[] _tempBuildings;
        private BuildingDemographic[] _finalBuildings;

        // building colors
        private static Color _neutralColor;
        private static Color _buildingColorLow;
        private static Color _buildingColorHigh;

        // for locking the thread while working with final buffer that is used by both the simulation thread and the UI thread
        private static readonly object _lockObject = new object();

        // miscellaneous
        private byte _selectedDistrictID = UIDistrictDropdown.DistrictIDEntireCity;
        private bool _initialized = false;
        private uint _citizenCounter = 0;
        private bool _triggerUpdatePanel = false;
        private UIPanel _populationLegend;
        private bool _hadronColliderEnabled;

        /// <summary>
        /// amounts for one data row
        /// </summary>
        private class DataRow
        {
            // one amount for each data column
            public int[] amount = new int[MaxColumns];

            // amounts for total, moving in, and deceased columns
            public int total;
            public int movingIn;
            public int deceased;
        }

        /// <summary>
        /// Start is called after the panel is created in Loading
        /// set up and populate the panel
        /// </summary>
        public override void Start()
        {
            // do base processing
            base.Start();

            try
            {
                // set panel properties
                name = "PopulationDemographicsPanel";
                backgroundSprite = "MenuPanel2";
                canFocus = true;

                // set initial visibility and opacity from config
                Configuration config = ConfigurationUtil<Configuration>.Load();
                isVisible = config.PanelVisible;
                opacity = config.PanelOpacity;

                // get the PopulationInfoViewPanel panel (displayed when the user clicks on the Population info view button)
                PopulationInfoViewPanel populationPanel = UIView.library.Get<PopulationInfoViewPanel>(nameof(PopulationInfoViewPanel));
                if (populationPanel == null)
                {
                    LogUtil.LogError("Unable to find PopulationInfoViewPanel.");
                    return;
                }

                // get legend from Population panel
                _populationLegend = populationPanel.Find<UIPanel>("Legend");
                if (_populationLegend == null)
                {
                    LogUtil.LogError("Unable to find Legend on PopulationInfoViewPanel.");
                    return;
                }

                // place panel to the right of PopulationInfoViewPanel
                relativePosition = new Vector3(populationPanel.component.size.x - 1f, 0f);

                // set panel to exact width to hold contained components
                // must do this before setting anchors on contained components
                autoSize = false;
                width = PanelTotalWidth;

                // get text font from the Population label because it is regular instead of semi-bold
                UILabel populationLabel = populationPanel.Find<UILabel>("Population");
                if (populationLabel == null)
                {
                    LogUtil.LogError("Unable to find Population label on PopulationInfoViewPanel.");
                    return;
                }
                UIFont textFont = populationLabel.font;

                // initialize row and column selection attributes
                if (!InitializeRowColumnSelections())
                {
                    return;
                }

                // get neutral color
                if (!InfoManager.exists)
                {
                    LogUtil.LogError("InfoManager is not ready.");
                    return;
                }
                _neutralColor = InfoManager.instance.m_properties.m_neutralColor;

                // get building low and high colors
                // high color is 50% between residential low and high density zone colors
                // low color is 15% between neutral and the high color
                if (!ZoneManager.exists)
                {
                    LogUtil.LogError("ZoneManager is not ready.");
                    return;
                }
                Color[] zoneColors = ZoneManager.instance.m_properties.m_zoneColors;
                _buildingColorHigh = Color.Lerp(zoneColors[(int)ItemClass.Zone.ResidentialLow], zoneColors[(int)ItemClass.Zone.ResidentialHigh], 0.5f);
                _buildingColorLow = Color.Lerp(_neutralColor, _buildingColorHigh, 0.15f);


                // for most of the UI elements added in the logic below,
                // anchors are used to automatically resize or move the elements when the panel size changes
                // the panel size changes based on row and column selections


                // create the title label
                UILabel title = AddUIComponent<UILabel>();
                if (title == null)
                {
                    LogUtil.LogError($"Unable to create title label.");
                    return;
                }
                title.name = "Title";
                title.font = textFont;
                title.text = "Demographics";
                title.textAlignment = UIHorizontalAlignment.Center;
                title.textScale = 1f;
                title.textColor = new Color32(254, 254, 254, 255);
                title.autoSize = false;
                title.size = new Vector2(width, 18f);
                title.relativePosition = new Vector3(0f, 11f);
                title.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top | UIAnchorStyle.Right;

                // create population icon in upper left
                UISprite panelIcon = AddUIComponent<UISprite>();
                if (panelIcon == null)
                {
                    LogUtil.LogError($"Unable to create population icon.");
                    return;
                }
                panelIcon.name = "PopulationIcon";
                panelIcon.autoSize = false;
                panelIcon.size = new Vector2(36f, 36f);
                panelIcon.relativePosition = new Vector3(10f, 2f);
                panelIcon.spriteName = "InfoIconPopulationPressed";

                // create close button in upper right
                UIButton closeButton = AddUIComponent<UIButton>();
                if (closeButton == null)
                {
                    LogUtil.LogError($"Unable to create close button.");
                    return;
                }
                closeButton.name = "CloseButton";
                closeButton.autoSize = false;
                closeButton.size = new Vector2(32f, 32f);
                closeButton.relativePosition = new Vector3(width - 34f, 2f);
                closeButton.anchor = UIAnchorStyle.Right | UIAnchorStyle.Top;
                closeButton.normalBgSprite = "buttonclose";
                closeButton.hoveredBgSprite = "buttonclosehover";
                closeButton.pressedBgSprite = "buttonclosepressed";


                // create district dropdown
                UIDistrictDropdown district = AddUIComponent<UIDistrictDropdown>();
                if (district == null || !district.initialized)
                {
                    LogUtil.LogError($"Unable to create district dropdown.");
                    return;
                }
                district.name = "DistrictSelection";
                district.dropdownHeight = DistrictItemHeight + 7f;
                district.font = textFont;
                district.textScale = TextScale;
                district.textColor = TextColorNormal;
                district.disabledTextColor = TextColorLocked;
                district.listHeight = 10 * (int)DistrictItemHeight + 8;
                district.itemHeight = (int)DistrictItemHeight;
                district.builtinKeyNavigation = true;
                district.relativePosition = new Vector3(PaddingWidth, TitleBarHeight + PaddingTop);
                district.autoSize = false;
                district.size = new Vector2(width - 2f * PaddingWidth, DistrictHeight);
                district.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top | UIAnchorStyle.Right;
                _selectedDistrictID = UIDistrictDropdown.DistrictIDEntireCity;
                district.selectedDistrictID = _selectedDistrictID;


                // create opacity slider from template on district panel
                if (!CreateOpacitySlider(district)) { return; }


                // create row selection label
                if (!CreateSelectionLabel(textFont, out UILabel rowSelectionLabel)) { return; }
                rowSelectionLabel.name = "RowSelectionLabel";
                rowSelectionLabel.text = "Row:";
                rowSelectionLabel.relativePosition = new Vector3(PaddingWidth, HeadingTop);

                // create row selection list
                string[] rowTexts = new string[_rowSelectionAttributes.Count];
                for (int r = 0; r < rowTexts.Length; r++)
                {
                    rowTexts[r] = _rowSelectionAttributes[(RowSelection)r].selectionText;
                }
                if (!CreateSelectionListBox(textFont, rowTexts, out UIListBox rowSelectionListBox)) { return; }
                rowSelectionListBox.name = "RowSelection";
                rowSelectionListBox.relativePosition = new Vector3(PaddingWidth, rowSelectionLabel.relativePosition.y + rowSelectionLabel.size.y);

                // create column selection label
                if (!CreateSelectionLabel(textFont, out UILabel columnSelectionLabel)) { return; }
                columnSelectionLabel.name = "ColumnSelectionLabel";
                columnSelectionLabel.text = "Column:";
                columnSelectionLabel.relativePosition = new Vector3(PaddingWidth, rowSelectionListBox.relativePosition.y + rowSelectionListBox.size.y + PaddingHeight);

                // create column selection list
                string[] columnTexts = new string[_columnSelectionAttributes.Count];
                for (int c = 0; c < columnTexts.Length; c++)
                {
                    columnTexts[c] = _columnSelectionAttributes[(ColumnSelection)c].selectionText;
                }
                if (!CreateSelectionListBox(textFont, columnTexts, out UIListBox columnSelectionListBox)) { return; }
                columnSelectionListBox.name = "ColumnSelection";
                columnSelectionListBox.relativePosition = new Vector3(PaddingWidth, columnSelectionLabel.relativePosition.y + columnSelectionLabel.size.y);

                // set initial row and column selections from config
                rowSelectionListBox   .selectedIndex = Math.Min(config.RowSelection,    rowSelectionListBox   .items.Length - 1);
                columnSelectionListBox.selectedIndex = Math.Min(config.ColumnSelection, columnSelectionListBox.items.Length - 1);

                // set panel to exact height to be just below column list box
                // must do this before setting anchors on subsequent contained components
                height = columnSelectionListBox.relativePosition.y + columnSelectionListBox.size.y + PaddingHeight;
                _panelHeightNotAge = height;    // remember this height

                // create panel to hold headings, heading lines, data scrollable panel, scroll bar, total lines, total row, moving in row, and deceased row
                UIPanel headingPanel = AddUIComponent<UIPanel>();
                if (headingPanel == null)
                {
                    LogUtil.LogError($"Unable to create heading panel.");
                    return;
                }
                headingPanel.name = "HeadingPanel";
                headingPanel.autoSize = false;
                headingPanel.size = new Vector2(width - rowSelectionListBox.relativePosition.x - rowSelectionListBox.size.x - PaddingWidth - ScrollbarWidth - PaddingWidth, 50f);
                headingPanel.relativePosition = new Vector3(rowSelectionListBox.relativePosition.x + rowSelectionListBox.size.x + PaddingWidth, rowSelectionLabel.relativePosition.y);
                headingPanel.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top | UIAnchorStyle.Right;

                // create heading row UI on heading panel
                if (!CreateDataRowUI(headingPanel, textFont, 0f, "HeadingDataRow", out _heading)) { return; }

                // adjust heading properties
                _heading.description.text = "";
                _heading.total.text = "Total";
                _heading.movingIn.text = "MovingIn";
                _heading.deceased.text = "Deceased";
                _heading.amountBar.isVisible = false;
                foreach (UILabel heading in _heading.amount)
                {
                    heading.textScale = TextScaleHeading;
                }
                _heading.total   .textScale = TextScaleHeading;
                _heading.movingIn.textScale = TextScaleHeading;
                _heading.deceased.textScale = TextScaleHeading;

                // create lines after headings
                if (!CreateLines(headingPanel, 15f, "HeadingLine", out _headingLines)) { return; }

                // set height of heading panel
                headingPanel.height = _headingLines.total.relativePosition.y + _headingLines.total.size.y + 2f;


                // create a panel to hold the scrollable panel, scroll bar, and totals
                _dataPanel = AddUIComponent<UIPanel>();
                if (_dataPanel == null)
                {
                    LogUtil.LogError($"Unable to create data panel.");
                    return;
                }
                _dataPanel.name = "DataPanel";
                _dataPanel.autoSize = false;
                _dataPanel.size = new Vector2(headingPanel.size.x + ScrollbarWidth, height - HeadingTop - headingPanel.size.y - PaddingHeight);
                _dataPanel.relativePosition = new Vector3(headingPanel.relativePosition.x, headingPanel.relativePosition.y + headingPanel.size.y);
                _dataPanel.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top | UIAnchorStyle.Right;

                // create scrollable panel to hold data rows panel
                // panel will be scrollable only when Age is selected for row
                // for other than Age, dataPanel will be sized so scrolling is not needed
                if (!CreateDataScrollablePanel(_dataPanel, out _dataScrollablePanel)) { return; }

                // create panel to hold the data rows
                _dataRowsPanel = _dataScrollablePanel.AddUIComponent<UIPanel>();
                if (_dataRowsPanel == null)
                {
                    LogUtil.LogError($"Unable to create data rows panel.");
                    return;
                }
                _dataRowsPanel.name = "DataRowsPanel";
                _dataRowsPanel.autoSize = false;
                _dataRowsPanel.size = new Vector2(headingPanel.size.x, MaxRows * TextHeight);
                _dataRowsPanel.relativePosition = new Vector3(headingPanel.relativePosition.x, headingPanel.relativePosition.y + headingPanel.size.y);
                _dataRowsPanel.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top | UIAnchorStyle.Right | UIAnchorStyle.Bottom;

                // create the data row UIs
                _dataRows = new DataRowUI[MaxRows];
                for (int r = 0; r < _dataRows.Length; r++)
                {
                    if (!CreateDataRowUI(_dataRowsPanel, textFont, r * TextHeight, "DataRow" + r, out _dataRows[r])) { return; }
                    _dataRows[r].description.text = r.ToString();
                }

                // create panel to hold totals
                UIPanel totalPanel = _dataPanel.AddUIComponent<UIPanel>();
                if (totalPanel == null)
                {
                    LogUtil.LogError($"Unable to create total panel.");
                    return;
                }
                totalPanel.name = "TotalPanel";
                totalPanel.autoSize = false;
                totalPanel.size = new Vector2(_dataPanel.size.x, HeightOfTotals);
                totalPanel.relativePosition = new Vector3(0f, _dataPanel.size.y - HeightOfTotals - SpaceAfterTotalsSection - HeightOfLegend);
                totalPanel.anchor = UIAnchorStyle.Left | UIAnchorStyle.Bottom | UIAnchorStyle.Right;

                // create lines above the totals
                float totalTop = 0;
                if (!CreateLines(totalPanel, totalTop, "TotalLine", out _totalLines)) { return; }

                // create total row UI
                totalTop += 4f;
                if (!CreateDataRowUI(totalPanel, textFont, totalTop, "Total", out _totalRow)) { return; }
                _totalRow.description.text = "Total";
                _totalRow.amountBar.isVisible = false;

                // create moving in row UI
                totalTop += TextHeight + 12f;
                if (!CreateDataRowUI(totalPanel, textFont, totalTop, "MovingIn", out _movingInRow)) { return; }
                _movingInRow.description.text = "Moving In";
                _movingInRow.amountBar.isVisible = false;

                // create deceased row UI
                totalTop += TextHeight;
                if (!CreateDataRowUI(totalPanel, textFont, totalTop, "Deceased", out _deceasedRow)) { return; }
                _deceasedRow.description.text = "Deceased";
                _deceasedRow.amountBar.isVisible = false;

                // hide duplicates for moving in and deceased, this leaves room for display options
                _movingInRow.movingIn.isVisible = false;
                _movingInRow.deceased.isVisible = false;
                _deceasedRow.movingIn.isVisible = false;
                _deceasedRow.deceased.isVisible = false;

                // create display option panels
                if (!CreateDisplayOptionPanel(totalPanel, textFont, _movingInRow.relativePosition.y, "CountOption",   "Count",   out _countPanel,   out _countCheckBox  )) { return; }
                if (!CreateDisplayOptionPanel(totalPanel, textFont, _deceasedRow.relativePosition.y, "PercentOption", "Percent", out _percentPanel, out _percentCheckBox)) { return; }

                // set initial count or percent from config
                SetCheckBox(config.CountStatus ? _countCheckBox : _percentCheckBox, true);

                // create legend panel
                if (!CreateLegendPanel(textFont)) { return; }

                // initialize final demographic buffers
                InitializeTempBuffersCounters();
                GetCitizenDemographicData((uint)CitizenManager.instance.m_citizens.m_buffer.Length);
                _finalCitizens = _tempCitizens;
                _finalBuildings = _tempBuildings;
                InitializeTempBuffersCounters();

                // update panel as if new column was selected
                ColumnSelectedIndexChanged(columnSelectionListBox, columnSelectionListBox.selectedIndex);

                // initialize cursor label font
                PopulationDemographicsLoading.cursorLabel.font = textFont;

                // set event handlers
                closeButton.eventClicked += CloseClicked;
                district.eventSelectedDistrictChanged += SelectedDistrictChanged;
                _opacitySlider.eventValueChanged += OpacityValueChanged;
                rowSelectionListBox   .eventSelectedIndexChanged += RowSelectedIndexChanged;
                columnSelectionListBox.eventSelectedIndexChanged += ColumnSelectedIndexChanged;
                _countPanel.eventClicked   += DisplayOption_eventClicked;
                _percentPanel.eventClicked += DisplayOption_eventClicked;
                eventVisibilityChanged += PanelVisibilityChanged;

                // panel is now initialized and ready for simulation ticks
                _initialized = true;
            }
            catch (Exception ex)
            {
                LogUtil.LogException(ex);
            }
        }


        #region Create UI

        /// <summary>
        /// initialize row and column selection attributes
        /// </summary>
        private bool InitializeRowColumnSelections()
        {
            // compute age group colors as slightly darker than the colors from the Population Info View panel
            PopulationInfoViewPanel populationPanel = UIView.library.Get<PopulationInfoViewPanel>(nameof(PopulationInfoViewPanel));
            if (populationPanel == null)
            {
                LogUtil.LogError("Unable to find PopulationInfoViewPanel.");
                return false;
            }
            const float ColorMultiplierAgeGroup = 0.7f;
            Color32 colorAgeGroupChild  = (Color)populationPanel.m_ChildColor  * ColorMultiplierAgeGroup;
            Color32 colorAgeGroupTeen   = (Color)populationPanel.m_TeenColor   * ColorMultiplierAgeGroup;
            Color32 colorAgeGroupYoung  = (Color)populationPanel.m_YoungColor  * ColorMultiplierAgeGroup;
            Color32 colorAgeGroupAdult  = (Color)populationPanel.m_AdultColor  * ColorMultiplierAgeGroup;
            Color32 colorAgeGroupSenior = (Color)populationPanel.m_SeniorColor * ColorMultiplierAgeGroup;

            // compute education level colors as slightly darker than the colors from the Education Info View panel
            EducationInfoViewPanel educationPanel = UIView.library.Get<EducationInfoViewPanel>(nameof(EducationInfoViewPanel));
            if (educationPanel == null)
            {
                LogUtil.LogError("Unable to find EducationInfoViewPanel.");
                return false;
            }
            const float ColorMultiplierEducation = 0.7f;
            Color32 colorEducationUneducated     = (Color)educationPanel.m_UneducatedColor     * ColorMultiplierEducation;
            Color32 colorEducationEducated       = (Color)educationPanel.m_EducatedColor       * ColorMultiplierEducation;
            Color32 colorEducationWellEducated   = (Color)educationPanel.m_WellEducatedColor   * ColorMultiplierEducation;
            Color32 colorEducationHighlyEducated = (Color)educationPanel.m_HighlyEducatedColor * ColorMultiplierEducation;

            // set employment colors to yellow, green, and blue
            Color32 colorEmploymentStudent    = new Color32(160, 160,  64, 255);
            Color32 colorEmploymentEmployed   = new Color32( 64, 192,  64, 255);
            Color32 colorEmploymentUnemployed = new Color32( 64,  64, 192, 255);

            // set gender colors to blue and red
            Color32 colorGenderMale   = new Color32( 64,  64, 192, 255);
            Color32 colorGenderFemale = new Color32(192,  64,  64, 255);

            // compute happiness colors as slightly darker than the colors from the Happiness Info View panel
            if (!InfoManager.exists)
            {
                LogUtil.LogError("InfoManager is not ready.");
                return false;
            }
            InfoProperties.ModeProperties happinessModeProperties = InfoManager.instance.m_properties.m_modeProperties[(int)InfoManager.InfoMode.Happiness];
            Color negativeHappinessColor = happinessModeProperties.m_negativeColor;
            Color targetHappinessColor   = happinessModeProperties.m_targetColor;
            const float ColorMultiplierHappiness = 0.8f;
            Color32 colorHappinessBad       = Color.Lerp(negativeHappinessColor, targetHappinessColor, 0.00f) * ColorMultiplierHappiness;
            Color32 colorHappinessPoor      = Color.Lerp(negativeHappinessColor, targetHappinessColor, 0.25f) * ColorMultiplierHappiness;
            Color32 colorHappinessGood      = Color.Lerp(negativeHappinessColor, targetHappinessColor, 0.50f) * ColorMultiplierHappiness;
            Color32 colorHappinessExcellent = Color.Lerp(negativeHappinessColor, targetHappinessColor, 0.75f) * ColorMultiplierHappiness;
            Color32 colorHappinessSuperb    = Color.Lerp(negativeHappinessColor, targetHappinessColor, 1.00f) * ColorMultiplierHappiness;

            // compute health colors as slightly darker than the colors from the Health Info View panel
            InfoProperties.ModeProperties healthModeProperties = InfoManager.instance.m_properties.m_modeProperties[(int)InfoManager.InfoMode.Health];
            Color32 negativeHealthColor = healthModeProperties.m_negativeColor;
            Color32 targetHealthColor   = healthModeProperties.m_targetColor;
            const float ColorMultiplierHealth = 0.8f;
            Color32 colorHealthVerySick    = Color.Lerp(negativeHealthColor, targetHealthColor, 0.0f) * ColorMultiplierHealth;
            Color32 colorHealthSick        = Color.Lerp(negativeHealthColor, targetHealthColor, 0.2f) * ColorMultiplierHealth;
            Color32 colorHealthPoor        = Color.Lerp(negativeHealthColor, targetHealthColor, 0.4f) * ColorMultiplierHealth;
            Color32 colorHealthHealthy     = Color.Lerp(negativeHealthColor, targetHealthColor, 0.6f) * ColorMultiplierHealth;
            Color32 colorHealthVeryHealthy = Color.Lerp(negativeHealthColor, targetHealthColor, 0.8f) * ColorMultiplierHealth;
            Color32 colorHealthExcellent   = Color.Lerp(negativeHealthColor, targetHealthColor, 1.0f) * ColorMultiplierHealth;

            // compute location colors as shades of orange
            // Hotel location is not used because only tourists, not citizens, are guests at hotels
            Color32 colorLocationBase = new Color32(254, 230, 177, 255);
            Color32 colorLocationHome     = (Color)colorLocationBase * 0.70f;
            Color32 colorLocationWork     = (Color)colorLocationBase * 0.65f;
            Color32 colorLocationVisiting = (Color)colorLocationBase * 0.60f;
            Color32 colorLocationMoving   = (Color)colorLocationBase * 0.55f;

            // compute residential colors based on neutral color and average of low and high density zone colors (i.e. similar to colors on Levels Info View panel)
            if (!ZoneManager.exists)
            {
                LogUtil.LogError("ZoneManager is not ready.");
                return false;
            }
            Color[] zoneColors = ZoneManager.instance.m_properties.m_zoneColors;
            Color color1 = Color.Lerp(zoneColors[(int)ItemClass.Zone.ResidentialLow], zoneColors[(int)ItemClass.Zone.ResidentialHigh], 0.5f);
            Color color0 = Color.Lerp(_neutralColor, color1, 0.20f);
            const float ColorMultiplierResidential = 0.8f;
            Color32 colorResidentialLevel1 = Color.Lerp(color0, color1, 0.00f) * ColorMultiplierResidential;
            Color32 colorResidentialLevel2 = Color.Lerp(color0, color1, 0.25f) * ColorMultiplierResidential;
            Color32 colorResidentialLevel3 = Color.Lerp(color0, color1, 0.50f) * ColorMultiplierResidential;
            Color32 colorResidentialLevel4 = Color.Lerp(color0, color1, 0.75f) * ColorMultiplierResidential;
            Color32 colorResidentialLevel5 = Color.Lerp(color0, color1, 1.00f) * ColorMultiplierResidential;

            // compute wealth colors as slightly darker than the colors from the Tourism Info View panel
            TourismInfoViewPanel tourismPanel = UIView.library.Get<TourismInfoViewPanel>(nameof(TourismInfoViewPanel));
            if (tourismPanel == null)
            {
                LogUtil.LogError("Unable to find TourismInfoViewPanel.");
                return false;
            }
            UIRadialChart wealthChart = tourismPanel.Find<UIRadialChart>("TouristWealthChart");
            if (wealthChart == null)
            {
                LogUtil.LogError("Unable to find TouristWealthChart.");
                return false;
            }
            const float ColorMultiplierWealth = 0.7f;
            Color32 colorWealthLow    = (Color)wealthChart.GetSlice(0).innerColor * ColorMultiplierWealth;
            Color32 colorWealthMedium = (Color)wealthChart.GetSlice(1).innerColor * ColorMultiplierWealth;
            Color32 colorWealthHigh   = (Color)wealthChart.GetSlice(2).innerColor * ColorMultiplierWealth;

            // compute well being colors as same range as health (there is no info view or other UI in the game for well being)
            const float ColorMultiplierWellBeing = 0.8f;
            Color32 colorWellBeingVerySad   = Color.Lerp(negativeHealthColor, targetHealthColor, 0.00f) * ColorMultiplierWellBeing;
            Color32 colorWellBeingSad       = Color.Lerp(negativeHealthColor, targetHealthColor, 0.25f) * ColorMultiplierWellBeing;
            Color32 colorWellBeingSatisfied = Color.Lerp(negativeHealthColor, targetHealthColor, 0.50f) * ColorMultiplierWellBeing;
            Color32 colorWellBeingHappy     = Color.Lerp(negativeHealthColor, targetHealthColor, 0.75f) * ColorMultiplierWellBeing;
            Color32 colorWellBeingVeryHappy = Color.Lerp(negativeHealthColor, targetHealthColor, 1.00f) * ColorMultiplierWellBeing;

            // set row selection attributes
            // the heading texts and amount bar colors must be defined in the same order as the corresponding Citizen enum
            // Hotel location is not used because only tourists, not citizens, are guests at hotels
            _rowSelectionAttributes = new RowSelectionAttributes
            {
                { RowSelection.Age,         new SelectionAttributes("Age",         null, null)   /* arrays for age get initialized below */                                                                                                                 },
                { RowSelection.AgeGroup,    new SelectionAttributes("Age Group",   new string[]  { "Children",               "Teens",                "Young Adults",             "Adults",                     "Seniors"                                    },
                                                                                   new Color32[] { colorAgeGroupChild,       colorAgeGroupTeen,      colorAgeGroupYoung,         colorAgeGroupAdult,           colorAgeGroupSenior                          }) },
                { RowSelection.Education,   new SelectionAttributes("Education",   new string[]  { "Uneducated",             "Educated",             "Well Educated",            "Highly Educated"                                                          },
                                                                                   new Color32[] { colorEducationUneducated, colorEducationEducated, colorEducationWellEducated, colorEducationHighlyEducated                                               }) },
                { RowSelection.Employment,  new SelectionAttributes("Employment",  new string[]  { "Student ",               "Employed",             "Jobless"                                                                                              },
                                                                                   new Color32[] { colorEmploymentStudent ,  colorEmploymentEmployed,colorEmploymentUnemployed                                                                              }) },
                { RowSelection.Gender,      new SelectionAttributes("Gender",      new string[]  { "Male",                   "Female"                                                                                                                       },
                                                                                   new Color32[] { colorGenderMale,          colorGenderFemale                                                                                                              }) },
                { RowSelection.Happiness,   new SelectionAttributes("Happiness",   new string[]  { "Bad",                    "Poor",                 "Good",                     "Excellent",                  "Superb"                                     },
                                                                                   new Color32[] { colorHappinessBad,        colorHappinessPoor,     colorHappinessGood,         colorHappinessExcellent,      colorHappinessSuperb                         }) },
                { RowSelection.Health,      new SelectionAttributes("Health",      new string[]  { "Very Sick",              "Sick",                 "Poor",                     "Healthy",                    "Very Healthy",         "Excellent"          },
                                                                                   new Color32[] { colorHealthVerySick,      colorHealthSick,        colorHealthPoor,            colorHealthHealthy,           colorHealthVeryHealthy, colorHealthExcellent }) },
                { RowSelection.Location,    new SelectionAttributes("Location",    new string[]  { "Home",                   "Work",                 "Visiting",                 "Moving"                                                                   },
                                                                                   new Color32[] { colorLocationHome,        colorLocationWork,      colorLocationVisiting,      colorLocationMoving                                                        }) },
                { RowSelection.Residential, new SelectionAttributes("Residential", new string[]  { "Level 1",                "Level 2",              "Level 3",                  "Level 4",                    "Level 5"                                    },
                                                                                   new Color32[] { colorResidentialLevel1,   colorResidentialLevel2, colorResidentialLevel3,     colorResidentialLevel4,       colorResidentialLevel5                       }) },
                { RowSelection.Student,     new SelectionAttributes("Student",     new string[]  { "Not a Student",          "Elementary",           "High School",              "University"                                                               },
                                                                                   new Color32[] { colorEducationUneducated, colorEducationEducated, colorEducationWellEducated, colorEducationHighlyEducated                                               }) },
                { RowSelection.Wealth,      new SelectionAttributes("Wealth",      new string[]  { "Low",                    "Medium",               "High"                                                                                                 },
                                                                                   new Color32[] { colorWealthLow,           colorWealthMedium,      colorWealthHigh                                                                                        }) },
                { RowSelection.WellBeing,   new SelectionAttributes("Well Being",  new string[]  { "Very Sad",               "Sad",                  "Satisfied",                "Happy",                      "Very Happy"                                 },
                                                                                   new Color32[] { colorWellBeingVerySad,    colorWellBeingSad,      colorWellBeingSatisfied,    colorWellBeingHappy,          colorWellBeingVeryHappy                      }) }
            };

            // initialize selection attribute arrays for age
            string[] ageHeadingTexts = new string[MaxRows];
            Color32[] ageAmountBarColors = new Color32[MaxRows];
            for (int r = 0; r < MaxRows; r++)
            {
                // initialize heading text
                ageHeadingTexts[r] = r.ToString();

                // initialize amount bar color based on color for corresponding age group
                switch (Citizen.GetAgeGroup((int)(r / RealAgePerGameAge)))
                {
                    case Citizen.AgeGroup.Child:   ageAmountBarColors[r] = colorAgeGroupChild;  break;
                    case Citizen.AgeGroup.Teen:    ageAmountBarColors[r] = colorAgeGroupTeen;   break;
                    case Citizen.AgeGroup.Young:   ageAmountBarColors[r] = colorAgeGroupYoung;  break;
                    case Citizen.AgeGroup.Adult:   ageAmountBarColors[r] = colorAgeGroupAdult;  break;
                    default:                       ageAmountBarColors[r] = colorAgeGroupSenior; break;
                }
            }
            _rowSelectionAttributes[RowSelection.Age].headingTexts = ageHeadingTexts;
            _rowSelectionAttributes[RowSelection.Age].amountBarColors = ageAmountBarColors;

            // set column attributes
            // the heading texts must be defined in the same order as the corresponding Citizen enum
            // Hotel location is not used because only tourists, not citizens, are guests at hotels
            _columnSelectionAttributes = new ColumnSelectionAttributes
            {
                { ColumnSelection.None,        new SelectionAttributes("None",        new string[] { /* intentionally empty array for None */                                        }, null) },
                { ColumnSelection.AgeGroup,    new SelectionAttributes("Age Group",   new string[] { "Children",   "Teens",    "YoungAdult", "Adults",    "Seniors"                  }, null) },
                { ColumnSelection.Education,   new SelectionAttributes("Education",   new string[] { "Uneducated", "Educated", "Well Edu",   "Highly Edu"                            }, null) },
                { ColumnSelection.Employment,  new SelectionAttributes("Employment",  new string[] { "Student",    "Employed", "Jobless"                                             }, null) },
                { ColumnSelection.Gender,      new SelectionAttributes("Gender",      new string[] { "Male",       "Female"                                                          }, null) },
                { ColumnSelection.Happiness,   new SelectionAttributes("Happiness",   new string[] { "Bad",        "Poor",     "Good",       "Excellent", "Superb"                   }, null) },
                { ColumnSelection.Health,      new SelectionAttributes("Health",      new string[] { "Very Sick",  "Sick",     "Poor",       "Healthy",   "VeryHealthy", "Excellent" }, null) },
                { ColumnSelection.Location,    new SelectionAttributes("Location",    new string[] { "Home",       "Work",     "Visiting",   "Moving"                                }, null) },
                { ColumnSelection.Residential, new SelectionAttributes("Residential", new string[] { "Level 1",    "Level 2",  "Level 3",    "Level 4",   "Level 5"                  }, null) },
                { ColumnSelection.Student,     new SelectionAttributes("Student",     new string[] { "NotStudent", "Elementary","HighSchool","University"                            }, null) },
                { ColumnSelection.Wealth,      new SelectionAttributes("Wealth",      new string[] { "Low",        "Medium",   "High"                                                }, null) },
                { ColumnSelection.WellBeing,   new SelectionAttributes("Well Being",  new string[] { "Very Sad",   "Sad",      "Satisfied",  "Happy",     "VeryHappy"                }, null) }
            };

            // success
            return true;
        }

        /// <summary>
        /// create label that goes above selection list box
        /// </summary>
        private bool CreateSelectionLabel(UIFont textFont, out UILabel selectionLabel)
        {
            // create the label on the demographics panel
            selectionLabel = AddUIComponent<UILabel>();
            if (selectionLabel == null)
            {
                LogUtil.LogError($"Unable to create selection label.");
                return false;
            }

            // set common properties
            selectionLabel.font = textFont;
            selectionLabel.textScale = TextScale;
            selectionLabel.textColor = TextColorNormal;
            selectionLabel.autoSize = false;
            selectionLabel.size = new Vector2(SelectionWidth, TextHeight);

            // success
            return true;
        }

        /// <summary>
        /// create selection list box
        /// </summary>
        private bool CreateSelectionListBox(UIFont textFont, string[] items, out UIListBox selectionListBox)
        {
            // create the list box on the demographics panel
            selectionListBox = AddUIComponent<UIListBox>();
            if (selectionListBox == null)
            {
                LogUtil.LogError($"Unable to create selection list box.");
                return false;
            }

            // set common properties
            selectionListBox.font = textFont;
            selectionListBox.textScale = TextScale;
            selectionListBox.itemTextColor = TextColorNormal;
            selectionListBox.normalBgSprite = "OptionsDropboxListbox";
            selectionListBox.itemHighlight = "ListItemHighlight";
            selectionListBox.itemHeight = 16;
            selectionListBox.itemPadding = new RectOffset(4, 0, 2, 2);
            selectionListBox.items = items;
            selectionListBox.autoSize = false;
            selectionListBox.size = new Vector2(SelectionWidth, selectionListBox.itemHeight * items.Length);

            // success
            return true;
        }

        /// <summary>
        /// create a data row UI
        /// </summary>
        private bool CreateDataRowUI(UIPanel onPanel, UIFont textFont, float top, string namePrefix, out DataRowUI dataRow)
        {
            // create new data panel
            dataRow = onPanel.AddUIComponent<DataRowUI>();
            dataRow.name = namePrefix + "Panel";
            dataRow.autoSize = false;
            dataRow.size = new Vector2(onPanel.size.x, TextHeight);
            dataRow.relativePosition = new Vector3(0f, top);
            dataRow.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top | UIAnchorStyle.Right;

            // create label for description
            dataRow.description = dataRow.AddUIComponent<UILabel>();
            if (dataRow.description == null)
            {
                LogUtil.LogError($"Unable to create description label for [{namePrefix}].");
                return false;
            }
            dataRow.description.name = namePrefix + "Description";
            dataRow.description.font = textFont;
            dataRow.description.text = "XXXXXXXXXXXX";
            dataRow.description.textAlignment = UIHorizontalAlignment.Left;
            dataRow.description.verticalAlignment = UIVerticalAlignment.Bottom;
            dataRow.description.textScale = TextScale;
            dataRow.description.textColor = TextColorNormal;
            dataRow.description.autoSize = false;
            dataRow.description.size = new Vector2(DescriptionWidth, TextHeight);
            dataRow.description.relativePosition = Vector3.zero;
            dataRow.description.isVisible = true;

            // create amount bar sprite
            dataRow.amountBar = dataRow.AddUIComponent<UISprite>();
            if (dataRow.amountBar == null)
            {
                LogUtil.LogError($"Unable to create amount bar for [{namePrefix}].");
                return false;
            }
            dataRow.amountBar.name = namePrefix + "AmountBar";
            dataRow.amountBar.relativePosition = Vector3.zero;
            dataRow.amountBar.spriteName = "EmptySprite";
            dataRow.amountBar.autoSize = false;
            dataRow.amountBar.size = new Vector2(dataRow.description.size.x, dataRow.description.size.y - 1f);
            dataRow.amountBar.fillDirection = UIFillDirection.Horizontal;
            dataRow.amountBar.fillAmount = 0f;
            dataRow.amountBar.isVisible = true;
            dataRow.amountBar.SendToBack();

            // create amount labels
            float left = dataRow.description.size.x + AmountSpacing;
            for (int c = 0; c < MaxColumns; c++)
            {
                if (!CreateAmountLabel(dataRow, textFont, left, namePrefix + "Column" + c, out dataRow.amount[c])) { return false; } left += AmountWidth + AmountSpacing;
            }

            // create labels for total, moving in, and deceased
            if (!CreateAmountLabel(dataRow, textFont, left, namePrefix + "Total",    out dataRow.total   )) { return false; } left += AmountWidth + AmountSpacingAfterTotal;
            if (!CreateAmountLabel(dataRow, textFont, left, namePrefix + "MovingIn", out dataRow.movingIn)) { return false; } left += AmountWidth + AmountSpacing;
            if (!CreateAmountLabel(dataRow, textFont, left, namePrefix + "Deceased", out dataRow.deceased)) { return false; }

            // set anchors
            dataRow.description.anchor = UIAnchorStyle.Left  | UIAnchorStyle.Top | UIAnchorStyle.Bottom;
            dataRow.amountBar  .anchor = UIAnchorStyle.Left  | UIAnchorStyle.Top | UIAnchorStyle.Bottom;
            dataRow.total      .anchor = UIAnchorStyle.Right | UIAnchorStyle.Top | UIAnchorStyle.Bottom;
            dataRow.movingIn   .anchor = UIAnchorStyle.Right | UIAnchorStyle.Top | UIAnchorStyle.Bottom;
            dataRow.deceased   .anchor = UIAnchorStyle.Right | UIAnchorStyle.Top | UIAnchorStyle.Bottom;
            for (int c = 0; c < MaxColumns; c++)
            {
                dataRow.amount[c].anchor = UIAnchorStyle.Left  | UIAnchorStyle.Top | UIAnchorStyle.Bottom;
            }

            // success
            return true;
        }

        /// <summary>
        /// create a label that displays an amount
        /// </summary>
        private bool CreateAmountLabel(UIPanel onPanel, UIFont textFont, float left, string labelName, out UILabel amount)
        {
            amount = onPanel.AddUIComponent<UILabel>();
            if (amount == null)
            {
                LogUtil.LogError($"Unable to create label [{labelName}].");
                return false;
            }
            amount.name = labelName;
            amount.font = textFont;
            amount.text = "0,000,000";
            amount.textAlignment = UIHorizontalAlignment.Right;
            amount.verticalAlignment = UIVerticalAlignment.Bottom;
            amount.textScale = TextScale;
            amount.textColor = TextColorNormal;
            amount.autoSize = false;
            amount.size = new Vector2(AmountWidth, TextHeight);
            amount.relativePosition = new Vector3(left, 0f);

            // success
            return true;
        }

        /// <summary>
        /// create UI lines
        /// use heading to get line positions
        /// </summary>
        private bool CreateLines(UIPanel onPanel, float top, string namePrefix, out LinesRowUI lines)
        {
            // create lines UI
            lines = new LinesRowUI();

            // create a line for each amount
            for (int c = 0; c < MaxColumns; c++)
            {
                if (!CreateLine(onPanel, _heading.amount[c].relativePosition.x, top, namePrefix + c, out lines.amount[c])) { return false; }
            }

            // create lines for total, moving in, and deceased
            if (!CreateLine(onPanel, _heading.total   .relativePosition.x, top, namePrefix + "Total",    out lines.total   )) { return false; }
            if (!CreateLine(onPanel, _heading.movingIn.relativePosition.x, top, namePrefix + "ModingIn", out lines.movingIn)) { return false; }
            if (!CreateLine(onPanel, _heading.deceased.relativePosition.x, top, namePrefix + "Deceased", out lines.deceased)) { return false; }

            // set anchors for total, moving in, and deceased
            lines.total   .anchor = UIAnchorStyle.Right | UIAnchorStyle.Top;
            lines.movingIn.anchor = UIAnchorStyle.Right | UIAnchorStyle.Top;
            lines.deceased.anchor = UIAnchorStyle.Right | UIAnchorStyle.Top;

            // success
            return true;
        }

        /// <summary>
        /// create a single UI line
        /// </summary>
        private bool CreateLine(UIPanel onPanel, float left, float top, string nameText, out UISprite line)
        {
            // create a line
            line = onPanel.AddUIComponent<UISprite>();
            if (line == null)
            {
                LogUtil.LogError($"Unable to create line sprite [{nameText}].");
                return false;
            }
            line.name = nameText;
            line.autoSize = false;
            line.size = new Vector2(AmountWidth, 2f);
            line.relativePosition = new Vector3(left + 2f, top);
            line.spriteName = "EmptySprite";
            line.color = LineColor;

            // success
            return true;
        }

        /// <summary>
        /// create the data scrollable panel on which the data rows panel will be created
        /// </summary>
        private bool CreateDataScrollablePanel(UIPanel onPanel, out UIScrollablePanel dataScrollablePanel)
        {
            // create scrollable panel
            // no need for autolayout because the scrollable panel will contain only one component
            dataScrollablePanel = onPanel.AddUIComponent<UIScrollablePanel>();
            if (dataScrollablePanel == null)
            {
                LogUtil.LogError($"Unable to create data scrollable panel.");
                return false;
            }
            dataScrollablePanel.name = "DataScrollablePanel";
            dataScrollablePanel.relativePosition = new Vector3(0f, 0f);
            dataScrollablePanel.size = new Vector2(onPanel.size.x, onPanel.size.y - HeightOfTotals - SpaceAfterTotalsSection - HeightOfLegend);
            dataScrollablePanel.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top | UIAnchorStyle.Right | UIAnchorStyle.Bottom;
            dataScrollablePanel.backgroundSprite = string.Empty;
            dataScrollablePanel.clipChildren = true;      // prevents contained components from being displayed when they are scrolled out of view
            dataScrollablePanel.scrollWheelDirection = UIOrientation.Vertical;
            dataScrollablePanel.builtinKeyNavigation = true;
            dataScrollablePanel.scrollWithArrowKeys = true;

            // create vertical scroll bar
            UIScrollbar verticalScrollbar = onPanel.AddUIComponent<UIScrollbar>();
            if (verticalScrollbar == null)
            {
                LogUtil.LogError($"Unable to create data scrollbar.");
                return false;
            }
            verticalScrollbar.name = "DataScrollbar";
            verticalScrollbar.size = new Vector2(ScrollbarWidth, dataScrollablePanel.size.y);
            verticalScrollbar.relativePosition = new Vector2(onPanel.width - ScrollbarWidth, 0f);
            verticalScrollbar.anchor = UIAnchorStyle.Top | UIAnchorStyle.Right | UIAnchorStyle.Bottom;
            verticalScrollbar.orientation = UIOrientation.Vertical;
            verticalScrollbar.stepSize = TextHeight;
            verticalScrollbar.incrementAmount = 3f * TextHeight;
            verticalScrollbar.scrollEasingType = ColossalFramework.EasingType.BackEaseOut;
            dataScrollablePanel.verticalScrollbar = verticalScrollbar;

            // create scroll bar track on scroll bar
            UISlicedSprite verticalScrollbarTrack = verticalScrollbar.AddUIComponent<UISlicedSprite>();
            if (verticalScrollbarTrack == null)
            {
                LogUtil.LogError($"Unable to create data scrollbar track.");
                return false;
            }
            verticalScrollbarTrack.name = "DataScrollbarTrack";
            verticalScrollbarTrack.size = new Vector2(ScrollbarWidth, dataScrollablePanel.size.y);
            verticalScrollbarTrack.relativePosition = Vector3.zero;
            verticalScrollbarTrack.anchor = UIAnchorStyle.Left | UIAnchorStyle.Top | UIAnchorStyle.Bottom;
            verticalScrollbarTrack.spriteName = "ScrollbarTrack";
            verticalScrollbar.trackObject = verticalScrollbarTrack;

            // create scroll bar thumb on scroll bar track
            UISlicedSprite verticalScrollbarThumb = verticalScrollbarTrack.AddUIComponent<UISlicedSprite>();
            if (verticalScrollbarThumb == null)
            {
                LogUtil.LogError($"Unable to create data scrollbar thumb.");
                return false;
            }
            verticalScrollbarThumb.name = "DataScrollbarThumb";
            verticalScrollbarThumb.autoSize = true;
            verticalScrollbarThumb.size = new Vector2(ScrollbarWidth - 4f, 0f);
            verticalScrollbarThumb.relativePosition = Vector3.zero;
            verticalScrollbarThumb.spriteName = "ScrollbarThumb";
            verticalScrollbar.thumbObject = verticalScrollbarThumb;

            // success
            return true;
        }

        /// <summary>
        /// create a panel to hold a display option
        /// </summary>
        private bool CreateDisplayOptionPanel(UIPanel onPanel, UIFont textFont, float top, string namePrefix, string labelText, out UIPanel panel, out UISprite checkBox)
        {
            // satisfy compiler
            checkBox = null;

            // create a new panel
            panel = onPanel.AddUIComponent<UIPanel>();
            if (panel == null)
            {
                LogUtil.LogError($"Unable to create panel [{namePrefix}].");
                return false;
            }
            panel.name = namePrefix + "Panel";
            panel.size = new Vector2(90f, TextHeight);
            panel.relativePosition = new Vector3(onPanel.size.x - ScrollbarWidth - panel.size.x - 10f, top);
            panel.anchor = UIAnchorStyle.Top | UIAnchorStyle.Right;
            panel.isVisible = true;

            // create the checkbox (i.e. a sprite)
            checkBox = panel.AddUIComponent<UISprite>();
            if (checkBox == null)
            {
                LogUtil.LogError($"Unable to create check box sprite on panel [{panel.name}].");
                return false;
            }
            checkBox.name = namePrefix + "CheckBox";
            checkBox.autoSize = false;
            checkBox.size = new Vector2(TextHeight, TextHeight);    // width is same as height
            checkBox.relativePosition = new Vector3(0f, 0f);
            SetCheckBox(checkBox, false);
            checkBox.isVisible = true;

            // create the label
            UILabel description = panel.AddUIComponent<UILabel>();
            if (description == null)
            {
                LogUtil.LogError($"Unable to create label on panel [{panel.name}].");
                return false;
            }
            description.name = namePrefix + "Text";
            description.font = textFont;
            description.text = labelText;
            description.textAlignment = UIHorizontalAlignment.Left;
            description.verticalAlignment = UIVerticalAlignment.Bottom;
            description.textScale = 0.875f;
            description.textColor = TextColorNormal;
            description.autoSize = false;
            description.size = new Vector2(panel.width - checkBox.size.x - 5f, TextHeight);
            description.relativePosition = new Vector3(checkBox.size.x + 5f, 2f);
            description.isVisible = true;

            // success
            return true;
        }

        /// <summary>
        /// return whether or not the check box is checked
        /// </summary>
        /// <param name="checkBox">the check box to check</param>
        private static bool IsCheckBoxChecked(UISprite checkBox)
        {
            return checkBox.spriteName == "check-checked";
        }

        /// <summary>
        /// set the check box status
        /// </summary>
        /// <param name="checkBox">the check box to set</param>
        /// <param name="value">the value to set for the check box</param>
        private static void SetCheckBox(UISprite checkBox, bool value)
        {
            // set check box to checked or unchecked
            if (value)
            {
                checkBox.spriteName = "check-checked";
            }
            else
            {
                checkBox.spriteName = "check-unchecked";
            }
        }

        /// <summary>
        /// create legend panel
        /// </summary>
        private bool CreateLegendPanel(UIFont textFont)
        {
            // create legend panel
            UIPanel legendPanel = _dataPanel.AddUIComponent<UIPanel>();
            if (legendPanel == null)
            {
                LogUtil.LogError($"Unable to create legend panel.");
                return false;
            }
            legendPanel.name = "DemographicLegendPanel";
            legendPanel.autoSize = false;
            legendPanel.size = new Vector2(_dataPanel.size.x, HeightOfLegend);
            legendPanel.relativePosition = new Vector3(0f, _dataPanel.size.y - HeightOfLegend);
            legendPanel.anchor = UIAnchorStyle.Left | UIAnchorStyle.Bottom | UIAnchorStyle.Right;
            legendPanel.isVisible = true;

            // create legend low value label
            const float ValueWidth = 50f;
            _legendLowValue = legendPanel.AddUIComponent<UILabel>();
            if (_legendLowValue == null)
            {
                LogUtil.LogError($"Unable to create low value label on legend panel.");
                return false;
            }
            _legendLowValue.name = "LowValue";
            _legendLowValue.font = textFont;
            _legendLowValue.text = "000.0";
            _legendLowValue.textAlignment = UIHorizontalAlignment.Center;
            _legendLowValue.autoSize = false;
            _legendLowValue.size = new Vector2(ValueWidth, TextHeight);
            _legendLowValue.relativePosition = new Vector3(0f, 2.5f);
            _legendLowValue.textColor = TextColorNormal;
            _legendLowValue.textScale = TextScale;
            _legendLowValue.tooltip = "Building with lowest value has this color and value";

            // create legend high value label
            _legendHighValue = legendPanel.AddUIComponent<UILabel>();
            if (_legendHighValue == null)
            {
                LogUtil.LogError($"Unable to create high value label on legend panel.");
                return false;
            }
            _legendHighValue.name = "HighValue";
            _legendHighValue.font = textFont;
            _legendHighValue.text = "000.0";
            _legendHighValue.textAlignment = UIHorizontalAlignment.Center;
            _legendHighValue.autoSize = false;
            _legendHighValue.size = new Vector2(ValueWidth, TextHeight);
            _legendHighValue.relativePosition = new Vector3(legendPanel.size.x - ValueWidth, 2.5f);
            _legendHighValue.textColor = TextColorNormal;
            _legendHighValue.textScale = TextScale;
            _legendHighValue.anchor = UIAnchorStyle.Top | UIAnchorStyle.Right;
            _legendHighValue.tooltip = "Building with highest value has this color and value";

            // create legend gradient
            UITextureSprite legendGradient = legendPanel.AddUIComponent<UITextureSprite>();
            if (legendGradient == null)
            {
                LogUtil.LogError($"Unable to create color gradient on legend panel.");
                return false;
            }
            legendGradient.name = "Gradient";
            legendGradient.autoSize = false;
            legendGradient.size = new Vector2(legendPanel.size.x - 2 * ValueWidth, TextHeight);
            legendGradient.relativePosition = new Vector3(ValueWidth, 0f);
            legendGradient.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left | UIAnchorStyle.Right;

            // get gradient material and texture using the Residential Gradient from the Levels info view panel as a template
            LevelsInfoViewPanel levelsPanel = UIView.library.Get<LevelsInfoViewPanel>(typeof(LevelsInfoViewPanel).Name);
            if (levelsPanel == null)
            {
                LogUtil.LogError("Unable to find LevelsInfoViewPanel.");
                return false;
            }
            UITextureSprite gradientTemplate = levelsPanel.Find<UITextureSprite>("ResidentialGradient");
            if (gradientTemplate == null)
            {
                LogUtil.LogError("Unable to find ResidentialGradient.");
                return false;
            }
            legendGradient.material = gradientTemplate.material;
            legendGradient.texture = gradientTemplate.texture;

            // set the gradient colors
            legendGradient.renderMaterial.SetColor("_ColorA", _buildingColorLow);
            legendGradient.renderMaterial.SetColor("_ColorB", _buildingColorHigh);
            legendGradient.renderMaterial.SetFloat("_Step", 0.01f);
            legendGradient.renderMaterial.SetFloat("_Scalar", 1f);
            legendGradient.renderMaterial.SetFloat("_Offset", 0f);

            // success
            return true;
        }

        /// <summary>
        /// create opacity slider and labels
        /// </summary>
        private bool CreateOpacitySlider(UIDistrictDropdown district)
        {
            UIPanel opacityPanel = district.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate")) as UIPanel;
            if (opacityPanel == null)
            {
                LogUtil.LogError($"Unable to attach opacity slider panel.");
                _opacitySlider = null;
                _opacityValueLabel = null;
                return false;
            }
            opacityPanel.name = "OpacityPanel";
            opacityPanel.autoSize = false;
            opacityPanel.autoLayout = false;
            const float OpacityLabelWidth = 60f;
            const float OpacitySliderWidth = 100f;
            const float OpacityValueLabelWidth = 40f;
            const float OpacitySpacing = 5f;
            opacityPanel.size = new Vector2(OpacityLabelWidth + OpacitySpacing + OpacitySliderWidth + OpacitySpacing + OpacityValueLabelWidth, TextHeight);
            opacityPanel.relativePosition = new Vector3(district.size.x - opacityPanel.size.x, 0f);
            opacityPanel.anchor = UIAnchorStyle.Top | UIAnchorStyle.Right;

            // get the label from the template
            UILabel sliderLabel = opacityPanel.Find<UILabel>("Label");
            if (sliderLabel == null)
            {
                LogUtil.LogError($"Unable to find opacity label.");
                _opacityValueLabel = null;
                return false;
            }
            sliderLabel.name = "OpacityLabel";
            sliderLabel.text = "Opacity: ";
            sliderLabel.autoSize = false;
            sliderLabel.size = new Vector2(OpacityLabelWidth, TextHeight);
            sliderLabel.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left;
            sliderLabel.relativePosition = new Vector3(0f, 2f);
            sliderLabel.textScale = TextScale;
            sliderLabel.textColor = TextColorNormal;
            sliderLabel.textAlignment = UIHorizontalAlignment.Right;

            // get the slider
            _opacitySlider = opacityPanel.Find<UISlider>("Slider");
            if (_opacitySlider == null)
            {
                LogUtil.LogError($"Unable to find opacity slider.");
                _opacityValueLabel = null;
                return false;
            }
            _opacitySlider.name = "OpacitySlider";
            _opacitySlider.autoSize = false;
            _opacitySlider.size = new Vector2(OpacitySliderWidth, TextHeight);
            _opacitySlider.relativePosition = new Vector3(sliderLabel.size.x + OpacitySpacing, 0f);
            _opacitySlider.orientation = UIOrientation.Horizontal;
            _opacitySlider.stepSize = 0.01f;
            _opacitySlider.scrollWheelAmount = 0.01f;
            _opacitySlider.minValue = 0.40f;
            _opacitySlider.maxValue = 1.00f;
            Configuration config = ConfigurationUtil<Configuration>.Load();
            _opacitySlider.value = Mathf.Clamp(config.PanelOpacity, _opacitySlider.minValue, _opacitySlider.maxValue);

            // create opacity value label
            _opacityValueLabel = opacityPanel.AddUIComponent<UILabel>();
            if (_opacityValueLabel == null)
            {
                LogUtil.LogError($"Unable to create opacity value label.");
                return false;
            }
            _opacityValueLabel.name = "OpacityValueLabel";
            _opacityValueLabel.autoSize = false;
            _opacityValueLabel.size = new Vector2(OpacityValueLabelWidth, TextHeight);
            _opacityValueLabel.relativePosition = new Vector3(_opacitySlider.relativePosition.x + _opacitySlider.size.x + OpacitySpacing, 2f);
            _opacityValueLabel.textScale = TextScale;
            _opacityValueLabel.textColor = TextColorNormal;
            _opacityValueLabel.textAlignment = UIHorizontalAlignment.Center;
            ShowOpacityValue(_opacitySlider.value);

            // success
            return true;
        }

        #endregion


        #region Event Handlers

        /// <summary>
        /// handle Close button clicked
        /// </summary>
        private void CloseClicked(UIComponent component, UIMouseEventParameter eventParam)
        {
            // hide this panel
            isVisible = false;
            Configuration.SavePanelVisible(isVisible);
        }

        /// <summary>
        /// handle panel visibility changed
        /// </summary>
        private void PanelVisibilityChanged(UIComponent component, bool value)
        {
            // population Legend visibility is opposite this panel's visibility
            _populationLegend.isVisible = !value;

            // check panel visibility
            if (value)
            {
                // trigger panel to update
                // this will eventually update all buildings
                _triggerUpdatePanel = true;
            }
            else
            {
                // update all buildings now
                BuildingManager.instance.UpdateBuildingColors();
            }
        }

        /// <summary>
        /// handle opacity slider value changed
        /// </summary>
        private void OpacityValueChanged(UIComponent component, float value)
        {
            // save value to config
            Configuration.SavePanelOpacity(value);

            // update panel opacity
            opacity = value;

            // show opacity value
            ShowOpacityValue(value);
        }

        /// <summary>
        /// show opacity value as a percent
        /// </summary>
        private void ShowOpacityValue(float opacity)
        {
            int opacityPercent = Mathf.RoundToInt(100f * opacity);
            _opacityValueLabel.text = opacityPercent.ToString() + "%";
        }

        /// <summary>
        /// handle change in district selection
        /// </summary>
        private void SelectedDistrictChanged(object sender, SelectedDistrictChangedEventArgs e)
        {
            // save selected district ID
            _selectedDistrictID = e.districtID;

            // trigger the panel to update
            _triggerUpdatePanel = true;
        }

        /// <summary>
        /// handle change in row selection
        /// </summary>
        private void RowSelectedIndexChanged(UIComponent component, int value)
        {
            // save selection to config
            Configuration.SaveRowSelection(value);

            // trigger the panel to update
            _triggerUpdatePanel = true;
        }

        /// <summary>
        /// handle change in column selection
        /// </summary>
        private void ColumnSelectedIndexChanged(UIComponent component, int value)
        {
            // save selection to config
            Configuration.SaveColumnSelection(value);

            // trigger the panel to update
            _triggerUpdatePanel = true;
        }

        /// <summary>
        /// handle Clicked event for display options
        /// </summary>
        private void DisplayOption_eventClicked(UIComponent component, UIMouseEventParameter eventParam)
        {
            // set check box that was clicked and clear the other check box
            SetCheckBox(_countCheckBox, (component == _countPanel));
            SetCheckBox(_percentCheckBox, (component == _percentPanel));

            // save count selection status to config
            Configuration.SaveCountStatus(IsCheckBoxChecked(_countCheckBox));

            // trigger the panel to update
            _triggerUpdatePanel = true;
        }

        #endregion


        #region Simulation Tick

        /// <summary>
        /// initialize temp buffers and counters for citizens and buildings
        /// </summary>
        private void InitializeTempBuffersCounters()
        {
            // initialize citizens
            _citizenCounter = 0;
            _tempCitizens = new CitizenDemographics();

            // initialize buildings and Hadron Collider status
            Building[] buildings = BuildingManager.instance.m_buildings.m_buffer;
            _tempBuildings = new BuildingDemographic[buildings.Length];
            _hadronColliderEnabled = false;
            for (int i = 0; i < _tempBuildings.Length; i++)
            {
                // create new building demographic
                _tempBuildings[i] = new BuildingDemographic();

                // initialize district
                Building building = buildings[i];
                _tempBuildings[i].districtID = DistrictManager.instance.GetDistrict(building.m_position);

                // get Hadron Collider status if not already enabled
                // there are two monuments in Africa in Miniature CCP that have HadronColliderAI, so need to check service
                BuildingInfo buildingInfo = building.Info;
                if (!_hadronColliderEnabled &&
                    buildingInfo != null &&
                    buildingInfo.m_buildingAI != null &&
                    buildingInfo.m_buildingAI.GetType() == typeof(HadronColliderAI) &&
                    ((building.m_flags & Building.Flags.Completed) != 0) &&
                    buildingInfo.m_class.m_service == ItemClass.Service.Education)
                {
                    // found Hadron Collider, save enabled status (mods allow more than one Hadron Collider)
                    // building is enabled when production rate is not 0, per logic adapted from PlayerBuildingAI.SimulationStepActive
                    _hadronColliderEnabled |= (building.m_productionRate != 0);
                }
            }
        }

        /// <summary>
        /// do processing for a simulation tick
        /// </summary>
        public void SimulationTick()
        {
            // panel must be initialized before processing
            // simulation ticks WILL occur before panel is initialized
            if (!_initialized)
            {
                return;
            }

            try
            {
                // managers must exit
                if (!CitizenManager.exists || !BuildingManager.exists || !DistrictManager.exists)
                {
                    LogUtil.LogError($"Managers ready: CitizenManager={CitizenManager.exists} BuildingManager={BuildingManager.exists} DistrictManager={DistrictManager.exists}.");
                    return;
                }

                // do a group of 8192 (i.e. 8K) citizens per tick
                // with the default buffer size of 1M, a full update of all citizens will be processed every 128 ticks
                // the table below shows that it will take 2.2 seconds for each update at the 3 different simulation speeds of the base game
                // some mods increase the simulation speed, which reduces the ticks/game day, which increases the days per full update
                // the game speed and city population do not change the number of ticks per real time
                // sim speed 1: 128 ticks / 585 ticks/game day = 0.22 game days/update * 10  sec/game day = 2.2 sec/update
                // sim speed 2: 128 ticks / 293 ticks/game day = 0.44 game days/update * 5   sec/game day = 2.2 sec/update
                // sim speed 3: 128 ticks / 145 ticks/game day = 0.88 game days/update * 2.5 sec/game day = 2.2 sec/update
                uint bufferSize = (uint)CitizenManager.instance.m_citizens.m_buffer.Length;
                uint lastCitizen = Math.Min(_citizenCounter + 8192, bufferSize);
                GetCitizenDemographicData(lastCitizen);

                // check for completed all groups
                if (_citizenCounter >= bufferSize)
                {
                    // compute demographics for each building
                    foreach (BuildingDemographic buildingDemographic in _tempBuildings)
                    {
                        // check for at least 1 citizen (prevents divide by zero)
                        if (buildingDemographic.citizenCount > 0)
                        {
                            // compute normal averages
                            buildingDemographic.avgAge          /= buildingDemographic.citizenCount;
                            buildingDemographic.avgEducation    /= buildingDemographic.citizenCount;
                            buildingDemographic.avgGender       /= buildingDemographic.citizenCount;
                            buildingDemographic.avgHappiness    /= buildingDemographic.citizenCount;
                            buildingDemographic.avgHealth       /= buildingDemographic.citizenCount;
                            buildingDemographic.avgWealth       /= buildingDemographic.citizenCount;
                            buildingDemographic.avgWellbeing    /= buildingDemographic.citizenCount;

                            // no need to compute average for Residential
                            // Residential already contains the building's level

                            // special average calculation for at home
                            buildingDemographic.avgAtHome = 100f * buildingDemographic.atHomeCount / buildingDemographic.citizenCount;

                            // calculate average for unemployed only if building has jobs eligible (prevents divide by zero)
                            if (buildingDemographic.jobEligibleCount > 0)
                            {
                                buildingDemographic.avgUnemployed = 100f * buildingDemographic.unemployedCount / buildingDemographic.jobEligibleCount;
                            }

                            // calculate average for student only if building has students (prevents divide by zero)
                            if (buildingDemographic.studentCount > 0)
                            {
                                buildingDemographic.avgStudent /= buildingDemographic.studentCount;
                            }
                        }
                    }

                    try
                    {
                        // lock thread while working with final buffers
                        LockThread();

                        // copy temp to final (final is used by the UI)
                        _finalCitizens = _tempCitizens;
                        _finalBuildings = _tempBuildings;
                    }
                    catch (Exception ex)
                    {
                        LogUtil.LogException(ex);
                    }
                    finally
                    {
                        // make sure thread is unlocked
                        UnlockThread();
                    }

                    // update panel with this new data
                    _triggerUpdatePanel = true;

                    // initialize buffers and counters to start over on next simulation tick
                    InitializeTempBuffersCounters();
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogException(ex);
            }
        }

        /// <summary>
        /// get the demographic data for a range of citizens
        /// </summary>
        private void GetCitizenDemographicData(uint lastCitizenID)
        {
            // get citizen and building buffers
            Citizen[] citizens = CitizenManager.instance.m_citizens.m_buffer;
            Building[] buildings = BuildingManager.instance.m_buildings.m_buffer;

            // do citizens from current counter to last citizen ID
            for (; _citizenCounter < lastCitizenID; _citizenCounter++)
            {
                // citizen must be created and must not be a tourist
                Citizen citizen = citizens[_citizenCounter];
                if ((citizen.m_flags & Citizen.Flags.Created) != 0 && (citizen.m_flags & Citizen.Flags.Tourist) == 0)
                {
                    // citizen must have a home building
                    if (citizen.m_homeBuilding != 0)
                    {
                        // home building must have an AI
                        Building homeBuilding = buildings[citizen.m_homeBuilding];
                        if (homeBuilding.Info != null && homeBuilding.Info.m_buildingAI != null)
                        {
                            // home building AI must be or derive from ResidentialBuildingAI   OR   home building AI must be OrphanageAI or NursingHomeAI from CimCare mod
                            // PloppableRICO.PloppableResidentialAI derives from PloppableRICO.GrowableResidentialAI which derives from ResidentialBuildingAI
                            Type homeBuildingAIType = homeBuilding.Info.m_buildingAI.GetType();
                            if (homeBuildingAIType == typeof(ResidentialBuildingAI) ||
                                homeBuildingAIType.IsSubclassOf(typeof(ResidentialBuildingAI)) ||
                                homeBuildingAIType.FullName.StartsWith("CimCareMod.AI.OrphanageAI") ||
                                homeBuildingAIType.FullName.StartsWith("CimCareMod.AI.NursingHomeAI"))
                            {
                                // set citizen demographic data
                                CitizenDemographic citizenDemographic = new CitizenDemographic();
                                citizenDemographic.citizenID   = _citizenCounter;
                                citizenDemographic.districtID  = DistrictManager.instance.GetDistrict(homeBuilding.m_position);
                                citizenDemographic.deceased    = ((citizen.m_flags & Citizen.Flags.Dead) != 0);
                                citizenDemographic.movingIn    = ((citizen.m_flags & Citizen.Flags.MovingIn) != 0);
                                citizenDemographic.age         = Mathf.Clamp(Mathf.RoundToInt(citizen.Age * RealAgePerGameAge), 0, MaxRealAge);
                                citizenDemographic.ageGroup    = Citizen.GetAgeGroup(citizen.Age);
                                citizenDemographic.education   = citizen.EducationLevel;
                                citizenDemographic.employment  = ((citizen.m_flags & Citizen.Flags.Student) != 0 ? EmploymentStatus.Student :
                                                                  (citizen.m_workBuilding != 0 ? EmploymentStatus.Employed : EmploymentStatus.Unemployed));
                                citizenDemographic.gender      = Citizen.GetGender(_citizenCounter);
                                citizenDemographic.happiness   = Citizen.GetHappinessLevel(Citizen.GetHappiness(citizen.m_health, citizen.m_wellbeing));
                                citizenDemographic.health      = Citizen.GetHealthLevel(citizen.m_health);
                                citizenDemographic.location    = citizen.CurrentLocation;
                                citizenDemographic.residential = homeBuilding.Info.m_class.m_level;
                                citizenDemographic.student     = citizen.GetCurrentSchoolLevel(_citizenCounter);
                                citizenDemographic.wealth      = citizen.WealthLevel;
                                citizenDemographic.wellbeing   = Citizen.GetWellbeingLevel(citizen.EducationLevel, citizen.m_wellbeing);

                                // save citizen demographic data
                                _tempCitizens.Add(citizenDemographic);

                                // building demographic is valid, even if no citizens are counted below
                                BuildingDemographic buildingDemographic = _tempBuildings[citizen.m_homeBuilding];
                                buildingDemographic.isValid = true;

                                // citizen must be not dead and not moving in to be included in building demographic
                                if (!citizenDemographic.deceased && !citizenDemographic.movingIn)
                                {
                                    // accumulate citizen demographic totals for this building
                                    buildingDemographic.citizenCount++;
                                    buildingDemographic.avgAge          += citizenDemographic.age;
                                    buildingDemographic.avgEducation    += (int)citizenDemographic.education;
                                    buildingDemographic.avgGender       += (int)citizenDemographic.gender;
                                    buildingDemographic.avgHappiness    += (int)citizenDemographic.happiness;
                                    buildingDemographic.avgHealth       += (int)citizenDemographic.health;
                                    buildingDemographic.avgWealth       += (int)citizenDemographic.wealth;
                                    buildingDemographic.avgWellbeing    += (int)citizenDemographic.wellbeing;

                                    // for Residential, don't accumulate a total, just save the building's level
                                    // add one to make it look like level is 1-5 instead of 0-4
                                    buildingDemographic.residential = ((int)citizenDemographic.residential) + 1;

                                    // count eligible to work
                                    // if Hadron Collider is built, then include teens
                                    if (citizenDemographic.ageGroup == Citizen.AgeGroup.Young ||
                                        citizenDemographic.ageGroup == Citizen.AgeGroup.Adult ||
                                        (_hadronColliderEnabled && citizenDemographic.ageGroup == Citizen.AgeGroup.Teen))
                                    {
                                        buildingDemographic.jobEligibleCount++;

                                        // count unemployed only if eligible to work
                                        if (citizenDemographic.employment == EmploymentStatus.Unemployed)
                                        {
                                            buildingDemographic.unemployedCount++;
                                        }
                                    }

                                    // count at home
                                    if (citizenDemographic.location == Citizen.Location.Home)
                                    {
                                        buildingDemographic.atHomeCount++;
                                    }

                                    // accumulate student demographics
                                    if (citizenDemographic.student != ItemClass.Level.None)
                                    {
                                        buildingDemographic.studentCount++;
                                        buildingDemographic.avgStudent += (float)citizenDemographic.student;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// lock thread while working with final buffer
        /// because the simulation thread writes to the final buffer and the UI thread reads from the final buffer
        /// </summary>
        private void LockThread()
        {
            Monitor.Enter(_lockObject);
        }

        /// <summary>
        /// unlock thread when done working with final buffer
        /// </summary>
        private void UnlockThread()
        {
            Monitor.Exit(_lockObject);
        }

        #endregion


        #region Update UI

        /// <summary>
        /// Update is called every frame, even when panel is not visible
        /// </summary>
        public override void Update()
        {
            // do base processing
            base.Update();

            try
            {
                // no need to update panel when it is not visible
                UILabel cursorLabel = PopulationDemographicsLoading.cursorLabel;
                if (!isVisible)
                {
                    cursorLabel.isVisible = false;
                    return;
                }

                // get row and column selections from config
                Configuration config = ConfigurationUtil<Configuration>.Load();
                RowSelection    rowSelection    = (RowSelection   )config.RowSelection;
                ColumnSelection columnSelection = (ColumnSelection)config.ColumnSelection;

                // the logic below to determine which building the cursor is over is adapted from a combination of:
                // ToolController.IsInsideUI
                // DefaultTool.OnToolLateUpdate
                // DefaultTool.SimulationStep
                // ToolBase.RayCast

                // check if cursor is inside UI
            	Vector3 mousePosition = Input.mousePosition;
                bool cursorIsInsideUI = mousePosition.x < 0f || mousePosition.x > Screen.width || mousePosition.y < 0f || mousePosition.y > Screen.height || UIView.IsInsideUI();

                // cursor must be not inside UI and must be visible
                bool cursorLabelVisible = false;
                if (!cursorIsInsideUI && Cursor.visible)
                {
                    // get input ray cast
                    Ray mouseRay = Camera.main.ScreenPointToRay(mousePosition);
                    float mouseRayLength = Camera.main.farClipPlane;
                	ToolBase.RaycastInput input = new ToolBase.RaycastInput(mouseRay, mouseRayLength);

                    // get building ID that cursor is over, if any
	                Vector3 origin = input.m_ray.origin;
	                Vector3 normalized = input.m_ray.direction.normalized;
	                Vector3 vector = input.m_ray.origin + normalized * input.m_length;
                    ColossalFramework.Math.Segment3 ray = new ColossalFramework.Math.Segment3(origin, vector);
                    if (BuildingManager.instance.RayCast(ray, ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.None, Building.Flags.None, out _, out ushort buildingID))
                    {
                        // make sure building ID is valid (not sure if it will ever be invalid)
                        if (buildingID != 0)
                        {
                            // if over untouchable building (not sure what that is), get parent building instead
		                    if (buildingID != 0 && (BuildingManager.instance.m_buildings.m_buffer[buildingID].m_flags & Building.Flags.Untouchable) != 0)
		                    {
			                    buildingID = Building.FindParentBuilding(buildingID);
		                    }

                            // cursor is over a building
                            try
                            {
                                // lock thread while working with final buffer
                                LockThread();

                                // check if this building's demographics are valid
                                BuildingDemographic buildingDemographic = _finalBuildings[buildingID];
                                if (buildingDemographic.isValid)
                                {
                                    // position cursor label below cursor
                                    UIView uIView = cursorLabel.GetUIView();
                                    Vector3 mousePosOnScreen = mousePosition / uIView.inputScale;
                                    Vector3 mousePosOnGUI = uIView.ScreenPointToGUI(mousePosOnScreen);
                                    cursorLabel.relativePosition = new Vector3(mousePosOnGUI.x, mousePosOnGUI.y + 18f);
                                    cursorLabel.isVisible = true;

                                    // get the building's demographic value for the selected column
                                    float value = 0f;
                                    switch (columnSelection)
                                    {
                                        case ColumnSelection.None:        value = buildingDemographic.citizenCount;  break;
                                        case ColumnSelection.AgeGroup:    value = buildingDemographic.avgAge;        break;
                                        case ColumnSelection.Education:   value = buildingDemographic.avgEducation;  break;
                                        case ColumnSelection.Employment:  value = buildingDemographic.avgUnemployed; break;
                                        case ColumnSelection.Gender:      value = buildingDemographic.avgGender;     break;
                                        case ColumnSelection.Happiness:   value = buildingDemographic.avgHappiness;  break;
                                        case ColumnSelection.Health:      value = buildingDemographic.avgHealth;     break;
                                        case ColumnSelection.Location:    value = buildingDemographic.avgAtHome;     break;
                                        case ColumnSelection.Residential: value = buildingDemographic.residential;   break;
                                        case ColumnSelection.Student:     value = buildingDemographic.avgStudent;    break;
                                        case ColumnSelection.Wealth:      value = buildingDemographic.avgWealth;     break;
                                        case ColumnSelection.WellBeing:   value = buildingDemographic.avgWellbeing;  break;
                                        default:
                                            LogUtil.LogError($"Unhandled column selection [{columnSelection}].");
                                            break;
                                    }

                                    // set cursor label text to the building's demographic value
                                    cursorLabel.text = value.ToString(NumberFormat(columnSelection), LocaleManager.cultureInfo);
                                    cursorLabelVisible = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                LogUtil.LogException(ex);
                            }
                            finally
                            {
                                // done getting data from final buffers
                                // make sure thread is unlocked
                                UnlockThread();
                            }
                        }
                    }
                }

                // set cursor label visibility
                cursorLabel.isVisible = cursorLabelVisible;

                // update the panel only when it is triggered for update
                if (_triggerUpdatePanel)
                {
                    // get selected row and column attributes
                    SelectionAttributes rowSelectionAttributes    = _rowSelectionAttributes   [rowSelection   ];
                    SelectionAttributes columnSelectionAttributes = _columnSelectionAttributes[columnSelection];

                    // get heading counts for selected row and column
                    int selectedRowCount    = rowSelectionAttributes   .headingTexts.Length;
                    int selectedColumnCount = columnSelectionAttributes.headingTexts.Length;

                    // show selected data rows and set headings
                    for (int r = 0; r < selectedRowCount; r++)
                    {
                        _dataRows[r].description.text = rowSelectionAttributes.headingTexts[r];
                        _dataRows[r].isVisible = true;
                    }

                    // hide extra data rows
                    for (int r = selectedRowCount; r < MaxRows; r++)
                    {
                        _dataRows[r].isVisible = false;
                    }

                    // show selected data columns and set headings
                    for (int c = 0; c < selectedColumnCount; c++)
                    {
                        _heading     .amount[c].text = columnSelectionAttributes.headingTexts[c];
                        _heading     .amount[c].isVisible = true;
                        _headingLines.amount[c].isVisible = true;
                        _totalLines  .amount[c].isVisible = true;
                        _totalRow    .amount[c].isVisible = true;
                        _movingInRow .amount[c].isVisible = true;
                        _deceasedRow .amount[c].isVisible = true;
                    }

                    // hide extra data columns
                    for (int c = selectedColumnCount; c < MaxColumns; c++)
                    {
                        _heading     .amount[c].isVisible = false;
                        _headingLines.amount[c].isVisible = false;
                        _totalLines  .amount[c].isVisible = false;
                        _totalRow    .amount[c].isVisible = false;
                        _movingInRow .amount[c].isVisible = false;
                        _deceasedRow .amount[c].isVisible = false;
                    }

                    // set panel width based on column count, which should cause anchors to move/resize everything else
                    width = PanelTotalWidth - (AmountWidth + AmountSpacing) * (MaxColumns - columnSelectionAttributes.headingTexts.Length);

                    // set heights based on row selection of Age or not Age
                    bool rowSelectionIsAge = (rowSelection == RowSelection.Age);
                    if (rowSelectionIsAge)
                    {
                        // set panel heights
                        height = PanelHeightForAge;
                        _dataPanel.height = PanelHeightForAge - _dataPanel.relativePosition.y - PaddingHeight;
                        _dataRowsPanel.height = TextHeightAge * MaxRows;

                        // show vertical scroll bar
                        _dataScrollablePanel.verticalScrollbar.isVisible = true;

                        // set row heights and text scales
                        for (int r = 0; r < MaxRows; r++)
                        {
                            DataRowUI dataRow = _dataRows[r];
                            dataRow.height = TextHeightAge;
                            dataRow.relativePosition = new Vector3(0f, r * TextHeightAge);
                            dataRow.description.textScale = TextScaleAge;
                            dataRow.total      .textScale = TextScaleAge;
                            dataRow.movingIn   .textScale = TextScaleAge;
                            dataRow.deceased   .textScale = TextScaleAge;
                            for (int c = 0; c < MaxColumns; c++)
                            {
                                dataRow.amount[c].textScale = TextScaleAge;
                            }
                        }
                    }
                    else
                    {
                        // set panel heights
                        height = _panelHeightNotAge;
                        _dataPanel.height = selectedRowCount * TextHeight + HeightOfTotals + SpaceAfterTotalsSection + HeightOfLegend;
                        _dataRowsPanel.height = selectedRowCount * TextHeight;

                        // hide vertical scroll bar
                        _dataScrollablePanel.verticalScrollbar.isVisible = false;

                        // set row heights and text scales
                        for (int r = 0; r < MaxRows; r++)
                        {
                            DataRowUI dataRow = _dataRows[r];
                            dataRow.height = TextHeight;
                            dataRow.relativePosition = new Vector3(0f, r * TextHeight);
                            dataRow.description.textScale = TextScale;
                            dataRow.total      .textScale = TextScale;
                            dataRow.movingIn   .textScale = TextScale;
                            dataRow.deceased   .textScale = TextScale;
                            for (int c = 0; c < MaxColumns; c++)
                            {
                                dataRow.amount[c].textScale = TextScale;
                            }
                        }
                    }

                    // define buffers to hold counts
                    DataRow[] rows = new DataRow[selectedRowCount];
                    DataRow total = new DataRow();
                    DataRow movingIn = new DataRow();
                    DataRow deceased = new DataRow();
                    for (int r = 0; r < rows.Length; r++)
                    {
                        rows[r] = new DataRow();
                    }

                    // gather selected data from final buffer
                    try
                    {
                        // lock thread while working with final buffer
                        LockThread();

                        // do each citizen
                        foreach (CitizenDemographic citizen in _finalCitizens)
                        {
                            // include citizen when selected district is Entire City OR selected district ID matches the citizen's district ID
                            if (_selectedDistrictID == UIDistrictDropdown.DistrictIDEntireCity || _selectedDistrictID == citizen.districtID)
                            {
                                // get row to increment
                                int row = 0;
                                switch (rowSelection)
                                {
                                    case RowSelection.Age:         row =      citizen.age;         break;
                                    case RowSelection.AgeGroup:    row = (int)citizen.ageGroup;    break;
                                    case RowSelection.Education:   row = (int)citizen.education;   break;
                                    case RowSelection.Employment:  row = (int)citizen.employment;  break;
                                    case RowSelection.Gender:      row = (int)citizen.gender;      break;
                                    case RowSelection.Happiness:   row = (int)citizen.happiness;   break;
                                    case RowSelection.Health:      row = (int)citizen.health;      break;
                                    case RowSelection.Location:    row = (int)citizen.location;    break;
                                    case RowSelection.Residential: row = (int)citizen.residential; break;
                                    case RowSelection.Student:     row = (int)citizen.student + 1; break;   // student starts at -1 for None
                                    case RowSelection.Wealth:      row = (int)citizen.wealth;      break;
                                    case RowSelection.WellBeing:   row = (int)citizen.wellbeing;   break;
                                    default:
                                        LogUtil.LogError($"Unhandled row selection [{rowSelection}].");
                                        break;
                                }

                                // get the column to increment
                                int column = 0;
                                switch (columnSelection)
                                {
                                    case ColumnSelection.None:        column = 0;                        break;     // increment column 0 even though it won't be displayed
                                    case ColumnSelection.AgeGroup:    column = (int)citizen.ageGroup;    break;
                                    case ColumnSelection.Education:   column = (int)citizen.education;   break;
                                    case ColumnSelection.Employment:  column = (int)citizen.employment;  break;
                                    case ColumnSelection.Gender:      column = (int)citizen.gender;      break;
                                    case ColumnSelection.Happiness:   column = (int)citizen.happiness;   break;
                                    case ColumnSelection.Health:      column = (int)citizen.health;      break;
                                    case ColumnSelection.Location:    column = (int)citizen.location;    break;
                                    case ColumnSelection.Residential: column = (int)citizen.residential; break;
                                    case ColumnSelection.Student:     column = (int)citizen.student + 1; break;     // student starts at -1 for None
                                    case ColumnSelection.Wealth:      column = (int)citizen.wealth;      break;
                                    case ColumnSelection.WellBeing:   column = (int)citizen.wellbeing;   break;
                                    default:
                                        LogUtil.LogError($"Unhandled column selection [{columnSelection}].");
                                        break;
                                }

                                // check if moving in
                                if (citizen.movingIn)
                                {
                                    // increment data row moving in, total row moving in, moving in row for the column, and moving in row total
                                    rows[row].movingIn++;
                                    total.movingIn++;
                                    movingIn.amount[column]++;
                                    movingIn.total++;
                                }
                                // check if deceased
                                else if (citizen.deceased)
                                {
                                    // increment data row deceased, total row deceased, deceased row for the column, and deceased row total
                                    rows[row].deceased++;
                                    total.deceased++;
                                    deceased.amount[column]++;
                                    deceased.total++;
                                }
                                else
                                {
                                    // increment data row for the column, data row total, total row for the column, and total row total
                                    rows[row].amount[column]++;
                                    rows[row].total++;
                                    total.amount[column]++;
                                    total.total++;      // this is the population of the selected district
                                }
                            }
                        }

                        // initialize each min and max building value
                        // min is set to the maximum possible value for the demographic
                        // max is set to the minimum possible value for the demographic
                        // add one to residential to make is look like it is 1-5 instead of 0-4
                        _minCitizens    = 99999;    // guessing there will never be more than 99,999 citizens in one building
                        _maxCitizens    = 0;
                        _minAge         = MaxRealAge;
                        _maxAge         = 0f;
                        _minEducation   = (float)Citizen.Education.ThreeSchools;
                        _maxEducation   = (float)Citizen.Education.Uneducated;
                        _minUnemployed  = 100f;     // unemployment rate
                        _maxUnemployed  = 0f;       // unemployment rate
                        _minGender      = (float)Citizen.Gender.Female;
                        _maxGender      = (float)Citizen.Gender.Male;
                        _minHappiness   = (float)Citizen.Happiness.Suberb;
                        _maxHappiness   = (float)Citizen.Happiness.Bad;
                        _minHealth      = (float)Citizen.Health.ExcellentHealth;
                        _maxHealth      = (float)Citizen.Health.VerySick;
                        _minAtHome      = 100f;     // percent at home
                        _maxAtHome      = 0f;       // percent at home
                        _minResidential = ((int)ItemClass.Level.Level5) + 1;
                        _maxResidential = ((int)ItemClass.Level.Level1) + 1;
                        _minStudent     = (float)ItemClass.Level.Level3;
                        _maxStudent     = (float)ItemClass.Level.Level1;
                        _minWealth      = (float)Citizen.Wealth.High;
                        _maxWealth      = (float)Citizen.Wealth.Low;
                        _minWellBeing   = (float)Citizen.Wellbeing.VeryHappy;
                        _maxWellBeing   = (float)Citizen.Wellbeing.VeryUnhappy;

                        // do each building
                        foreach (BuildingDemographic buildingDemographic in _finalBuildings)
                        {
                            // include building when selected district is Entire City OR selected district ID matches the building's district ID
                            if (_selectedDistrictID == UIDistrictDropdown.DistrictIDEntireCity || _selectedDistrictID == buildingDemographic.districtID)
                            {
                                // update min/max only for buildings with at least 1 citizen
                                if (buildingDemographic.citizenCount > 0)
                                {
                                    // update normal min and max values
                                    _minCitizens    = Mathf.Min(_minCitizens,    buildingDemographic.citizenCount);
                                    _maxCitizens    = Mathf.Max(_maxCitizens,    buildingDemographic.citizenCount);
                                    _minAge         = Mathf.Min(_minAge,         buildingDemographic.avgAge);
                                    _maxAge         = Mathf.Max(_maxAge,         buildingDemographic.avgAge);
                                    _minEducation   = Mathf.Min(_minEducation,   buildingDemographic.avgEducation);
                                    _maxEducation   = Mathf.Max(_maxEducation,   buildingDemographic.avgEducation);
                                    _minGender      = Mathf.Min(_minGender,      buildingDemographic.avgGender);
                                    _maxGender      = Mathf.Max(_maxGender,      buildingDemographic.avgGender);
                                    _minHappiness   = Mathf.Min(_minHappiness,   buildingDemographic.avgHappiness);
                                    _maxHappiness   = Mathf.Max(_maxHappiness,   buildingDemographic.avgHappiness);
                                    _minHealth      = Mathf.Min(_minHealth,      buildingDemographic.avgHealth);
                                    _maxHealth      = Mathf.Max(_maxHealth,      buildingDemographic.avgHealth);
                                    _minAtHome      = Mathf.Min(_minAtHome,      buildingDemographic.avgAtHome);
                                    _maxAtHome      = Mathf.Max(_maxAtHome,      buildingDemographic.avgAtHome);
                                    _minResidential = Mathf.Min(_minResidential, buildingDemographic.residential);
                                    _maxResidential = Mathf.Max(_maxResidential, buildingDemographic.residential);
                                    _minWealth      = Mathf.Min(_minWealth,      buildingDemographic.avgWealth);
                                    _maxWealth      = Mathf.Max(_maxWealth,      buildingDemographic.avgWealth);
                                    _minWellBeing   = Mathf.Min(_minWellBeing,   buildingDemographic.avgWellbeing);
                                    _maxWellBeing   = Mathf.Max(_maxWellBeing,   buildingDemographic.avgWellbeing);

                                    // update min/max unemployed only if building has jobs eligible
                                    if (buildingDemographic.jobEligibleCount > 0)
                                    {
                                        _minUnemployed = Mathf.Min(_minUnemployed, buildingDemographic.avgUnemployed);
                                        _maxUnemployed = Mathf.Max(_maxUnemployed, buildingDemographic.avgUnemployed);
                                    }

                                    // update min/max student only if building has students
                                    if (buildingDemographic.studentCount > 0)
                                    {
                                        _minStudent = Mathf.Min(_minStudent, buildingDemographic.avgStudent);
                                        _maxStudent = Mathf.Max(_maxStudent, buildingDemographic.avgStudent);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogUtil.LogException(ex);
                    }
                    finally
                    {
                        // done getting data from final buffers
                        // make sure thread is unlocked
                        UnlockThread();
                    }

                    // get the maximum total amongst selected rows
                    // the amount bar for every row is displayed in proportion to this value
                    int maxRowTotal = 0;
                    for (int r = 0; r < selectedRowCount; r++)
                    {
                        if (rows[r].total > maxRowTotal)
                        {
                            maxRowTotal = rows[r].total;
                        }
                    }

                    // display each selected data row
                    bool countIsSelected = IsCheckBoxChecked(_countCheckBox);
                    for (int r = 0; r < selectedRowCount; r++)
                    {
                        DisplayDataRow(_dataRows[r], rows[r], countIsSelected, rowSelectionIsAge, selectedColumnCount, total.total, movingIn.total, deceased.total, maxRowTotal, rowSelectionAttributes.amountBarColors[r]);
                    }

                    // display total rows
                    DisplayDataRow(_totalRow,    total,    countIsSelected, rowSelectionIsAge, selectedColumnCount, total   .total, movingIn.total, deceased.total, 0, Color.black);
                    DisplayDataRow(_movingInRow, movingIn, countIsSelected, rowSelectionIsAge, selectedColumnCount, movingIn.total, 0,              0,              0, Color.black);
                    DisplayDataRow(_deceasedRow, deceased, countIsSelected, rowSelectionIsAge, selectedColumnCount, deceased.total, 0,              0,              0, Color.black);

                    // get low and high values to display based on column selection
                    float lowValue = 0f;
                    float highValue = 0f;
                    switch ((ColumnSelection)config.ColumnSelection)
                    {
                        case ColumnSelection.None:        lowValue = _minCitizens;    highValue = _maxCitizens;    break;
                        case ColumnSelection.AgeGroup:    lowValue = _minAge;         highValue = _maxAge;         break;
                        case ColumnSelection.Education:   lowValue = _minEducation;   highValue = _maxEducation;   break;
                        case ColumnSelection.Employment:  lowValue = _minUnemployed;  highValue = _maxUnemployed;  break;
                        case ColumnSelection.Gender:      lowValue = _minGender;      highValue = _maxGender;      break;
                        case ColumnSelection.Happiness:   lowValue = _minHappiness;   highValue = _maxHappiness;   break;
                        case ColumnSelection.Health:      lowValue = _minHealth;      highValue = _maxHealth;      break;
                        case ColumnSelection.Location:    lowValue = _minAtHome;      highValue = _maxAtHome;      break;
                        case ColumnSelection.Residential: lowValue = _minResidential; highValue = _maxResidential; break;
                        case ColumnSelection.Student:     lowValue = _minStudent;     highValue = _maxStudent;     break;
                        case ColumnSelection.Wealth:      lowValue = _minWealth;      highValue = _maxWealth;      break;
                        case ColumnSelection.WellBeing:   lowValue = _minWellBeing;   highValue = _maxWellBeing;   break;

                        default:
                            Debug.LogError($"Unhandled column selection [{(ColumnSelection)config.ColumnSelection}] while displaying low/high values.");
                            break;
                    }

                    // if low value is more than high value, then swap low and high values
                    // this can happen when there are no citizens (e.g. a new city)
                    if (lowValue > highValue)
                    {
                        float tempValue = lowValue;
                        lowValue = highValue;
                        highValue = tempValue;
                    }

                    // display the low and high values on the legend
                    string numberFormat = NumberFormat(columnSelection);
                    _legendLowValue.text = lowValue.ToString(numberFormat, LocaleManager.cultureInfo);
                    _legendHighValue.text = highValue.ToString(numberFormat, LocaleManager.cultureInfo);

                    // update all buildings with this new data
                    BuildingManager.instance.UpdateBuildingColors();

                    // wait for next trigger
                    _triggerUpdatePanel = false;
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogException(ex);
            }
        }

        /// <summary>
        /// display a data row on the UI
        /// </summary>
        private void DisplayDataRow(
            DataRowUI dataRowUI,
            DataRow dataRow,
            bool countIsSelected,
            bool rowSelectionIsAge,
            int selectedColumnCount,
            int totalTotal,
            int totalMovingIn,
            int totalDeceased,
            int maxRowTotal,
            Color32 amountBarColor)
        {
            // set amount bar amount
            if (maxRowTotal == 0)
            {
                dataRowUI.amountBar.fillAmount = 0f;
            }
            else
            {
                dataRowUI.amountBar.fillAmount = (float)dataRow.total / maxRowTotal;
            }

            // set amount bar color
            dataRowUI.amountBar.color = amountBarColor;

            // check if count or percent
            if (countIsSelected)
            {
                // display counts
                for (int c = 0; c < selectedColumnCount; c++)
                {
                    dataRowUI.amount[c].text = dataRow.amount[c].ToString("N0", LocaleManager.cultureInfo);
                    dataRowUI.amount[c].isVisible = true;
                }
                dataRowUI.total    .text = dataRow.total   .ToString("N0", LocaleManager.cultureInfo);
                dataRowUI.movingIn .text = dataRow.movingIn.ToString("N0", LocaleManager.cultureInfo);
                dataRowUI.deceased .text = dataRow.deceased.ToString("N0", LocaleManager.cultureInfo);
            }
            else
            {
                // display percents
                string format = (rowSelectionIsAge ? "F3" : "F0");
                for (int c = 0; c < selectedColumnCount; c++)
                {
                    dataRowUI.amount[c].text = FormatPercent(dataRow.amount[c], totalTotal, format);
                    dataRowUI.amount[c].isVisible = true;
                }
                dataRowUI.total    .text = FormatPercent(dataRow.total,    totalTotal,    format);
                dataRowUI.movingIn .text = FormatPercent(dataRow.movingIn, totalMovingIn, format);
                dataRowUI.deceased .text = FormatPercent(dataRow.deceased, totalDeceased, format);
            }

            // hide extra columns
            for (int c = selectedColumnCount; c < MaxColumns; c++)
            {
                dataRowUI.amount[c].isVisible = false;
            }
        }

        /// <summary>
        /// format a value as percent of a total
        /// </summary>
        private string FormatPercent(int value, int total, string format)
        {
            float percent = 0f;
            if (total != 0)
            {
                percent = 100f * value / total;
            }
            return percent.ToString(format, LocaleManager.cultureInfo);
        }

        /// <summary>
        /// get the number format for building demographics
        /// </summary>
        private string NumberFormat(ColumnSelection columnSelection)
        {
            // display None (i.e. citizen count) and residential level as integers
            if (columnSelection == ColumnSelection.None || columnSelection == ColumnSelection.Residential)
            {
                return "N0";
            }

            // display all others with 1 decimal place
            return "N1";
        }

        /// <summary>
        /// get building color based on selected column and building population demographics
        /// </summary>
        /// <returns>whether or not to do base processing</returns>
        public bool GetBuildingColor(ushort buildingID, ref Building data, ref Color buildingColor)
        {
            // home building must be completed, not abandoned, and not collapsed
            // logic adapted from ResidentialBuildingAI.GetColor
            if ((data.m_flags & (Building.Flags.Completed | Building.Flags.Abandoned | Building.Flags.Collapsed)) == Building.Flags.Completed)
            {
                try
                {
                    // lock thread while working with final buffers
                    LockThread();

                    // color building when selected district is Entire City OR selected district ID matches the building's district ID
                    BuildingDemographic buildingDemographic = _finalBuildings[buildingID];
                    if (_selectedDistrictID == UIDistrictDropdown.DistrictIDEntireCity || _selectedDistrictID == buildingDemographic.districtID)
                    {
                        // check column selection
                        Configuration config = ConfigurationUtil<Configuration>.Load();
                        switch ((ColumnSelection)config.ColumnSelection)
                        {
                            // get building color according to building demographics
                            case ColumnSelection.None:        buildingColor = GetBuildingColor(buildingDemographic.citizenCount,   _minCitizens,    _maxCitizens   ); return false;
                            case ColumnSelection.AgeGroup:    buildingColor = GetBuildingColor(buildingDemographic.avgAge,         _minAge,         _maxAge        ); return false;
                            case ColumnSelection.Education:   buildingColor = GetBuildingColor(buildingDemographic.avgEducation,   _minEducation,   _maxEducation  ); return false;
                            case ColumnSelection.Gender:      buildingColor = GetBuildingColor(buildingDemographic.avgGender,      _minGender,      _maxGender     ); return false;
                            case ColumnSelection.Happiness:   buildingColor = GetBuildingColor(buildingDemographic.avgHappiness,   _minHappiness,   _maxHappiness  ); return false;
                            case ColumnSelection.Health:      buildingColor = GetBuildingColor(buildingDemographic.avgHealth,      _minHealth,      _maxHealth     ); return false;
                            case ColumnSelection.Location:    buildingColor = GetBuildingColor(buildingDemographic.avgAtHome,      _minAtHome,      _maxAtHome     ); return false;
                            case ColumnSelection.Residential: buildingColor = GetBuildingColor(buildingDemographic.residential,    _minResidential, _maxResidential); return false;
                            case ColumnSelection.Wealth:      buildingColor = GetBuildingColor(buildingDemographic.avgWealth,      _minWealth,      _maxWealth     ); return false;
                            case ColumnSelection.WellBeing:   buildingColor = GetBuildingColor(buildingDemographic.avgWellbeing,   _minWellBeing,   _maxWellBeing  ); return false;

                            case ColumnSelection.Employment:
                                if (buildingDemographic.jobEligibleCount > 0)
                                {
                                    // building has job eligible citizens, get unemployment color normally
                                    buildingColor = GetBuildingColor(buildingDemographic.avgUnemployed, _minUnemployed, _maxUnemployed);
                                }
                                else
                                {
                                    // building has no job eligible citizens, use low color
                                    buildingColor = _buildingColorLow;
                                }
                                return false;

                            case ColumnSelection.Student:
                                if (buildingDemographic.studentCount > 0)
                                {
                                    // building has students, get student color normally
                                    buildingColor = GetBuildingColor(buildingDemographic.avgStudent, _minStudent, _maxStudent);
                                }
                                else
                                {
                                    // building has no students, use low color
                                    buildingColor = _buildingColorLow;
                                }
                                return false;

                            default:
                                Debug.LogError($"Unhandled column selection [{(ColumnSelection)config.ColumnSelection}] while getting building color.");
                                return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.LogException(ex);
                    return true;
                }
                finally
                {
                    // make sure thread is unlocked
                    UnlockThread();
                }
            }

            // building does not meet any conditions above to get color, use neutral color
            buildingColor = _neutralColor;
            return false;
        }

        /// <summary>
        /// get building color based on building demographics
        /// </summary>
        private static Color GetBuildingColor(float value, float minValue, float maxValue)
        {
            // if no citizens (i.e. min and max were never updated from initial values), use low color
            if (minValue > maxValue)
            {
                return _buildingColorLow;
            }

            // if all citizens in all buildings have same value, use high color
            // this logic prevents divide by zero in normal case below
            if (minValue == maxValue)
            {
                return _buildingColorHigh;
            }

            // normal case, use proportional color with limit between 0 and 1
            float proportion = Mathf.Clamp01((value - minValue) / (maxValue - minValue));
            return Color.Lerp(_buildingColorLow, _buildingColorHigh, proportion);
        }

        #endregion
    }
}
