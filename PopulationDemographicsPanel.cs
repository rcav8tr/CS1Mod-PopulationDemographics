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

        // UI elements for count/percent buttons
        private UIPanel _countPanel;
        private UIPanel _percentPanel;
        private UISprite _countCheckBox;
        private UISprite _percentCheckBox;


        // here is the hierarchy of UI elements:
        //
        //  PopulationDemographicsPanel
        //      population icon
        //      title label
        //      close button
        //      district dropdown
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
            public bool                 deceasesd;
            public bool                 movingIn;
            public int                  age;        // real age, not game age
            public Citizen.AgeGroup     ageGroup;
            public Citizen.Education    education;
            public EmploymentStatus     employment;
            public Citizen.Gender       gender;
            public Citizen.Happiness    happiness;
            public Citizen.Health       health;
            public Citizen.Location     location;
            public ItemClass.Level      residential;
            public ItemClass.Level      student;    // None (i.e. -1) = not a student, Levels 1-3 (i.e. 0-2) = Elementary, High School, University
            public Citizen.Wealth       wealth;
            public Citizen.Wellbeing    wellbeing;
        }

        /// <summary>
        /// the demographic data for a collection of citizens
        /// </summary>
        private class CitizenDemographics : List<CitizenDemographic> { }

        // the demographics buffers
        // temp buffer gets populated during simulation ticks
        // final buffer gets updated periodically from temp buffer and is used to display the demographics
        private CitizenDemographics _tempCitizens;
        private CitizenDemographics _finalCitizens;

        // for locking the thread while working with final buffer that is used by both the simulation thread and the UI thread
        private static readonly object _lockObject = new object();

        // miscellaneous
        private byte _selectedDistrictID = UIDistrictDropdown.DistrictIDEntireCity;
        private bool _initialized = false;
        private uint _citizenCounter = 0;
        private bool _triggerUpdatePanel = false;

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
                opacity = 1f;

                // set initial visibility from config
                Configuration config = ConfigurationUtil<Configuration>.Load();
                isVisible = config.PanelVisible;

                // get the PopulationInfoViewPanel panel (displayed when the user clicks on the Population info view button)
                PopulationInfoViewPanel populationPanel = UIView.library.Get<PopulationInfoViewPanel>(nameof(PopulationInfoViewPanel));
                if (populationPanel == null)
                {
                    LogUtil.LogError("Unable to find PopulationInfoViewPanel.");
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
                totalPanel.name = "TotalPanel";
                totalPanel.autoSize = false;
                totalPanel.size = new Vector2(_dataPanel.size.x, HeightOfTotals);
                totalPanel.relativePosition = new Vector3(0f, _dataPanel.size.y - HeightOfTotals);
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

                // set event handlers
                closeButton.eventClicked += CloseClicked;
                district.eventSelectedDistrictChanged += SelectedDistrictChanged;
                rowSelectionListBox   .eventSelectedIndexChanged += RowSelectedIndexChanged;
                columnSelectionListBox.eventSelectedIndexChanged += ColumnSelectedIndexChanged;
                _countPanel.eventClicked   += DisplayOption_eventClicked;
                _percentPanel.eventClicked += DisplayOption_eventClicked;

                // initialize citizen demographics
                _tempCitizens = new CitizenDemographics();
                if (CitizenManager.exists && BuildingManager.exists && DistrictManager.exists)
                {
                    for (uint citizenID = 0; citizenID < CitizenManager.instance.m_citizens.m_buffer.Length; citizenID++)
                    {
                        CitizenDemographic citizenDemographic = GetCitizenDemographicData(citizenID);
                        if (citizenDemographic != null)
                        {
                            _tempCitizens.Add(citizenDemographic);
                        }
                    }
                }

                // copy temp to final
                _finalCitizens = _tempCitizens;
                _tempCitizens = new CitizenDemographics();
                _triggerUpdatePanel = true;

                // initialize citizen counter for simulation tick
                _citizenCounter = 0;

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
            Color colorNeutral = InfoManager.instance.m_properties.m_neutralColor;
            Color[] zoneColors = ZoneManager.instance.m_properties.m_zoneColors;
            Color color1 = Color.Lerp(zoneColors[(int)ItemClass.Zone.ResidentialLow], zoneColors[(int)ItemClass.Zone.ResidentialHigh], 0.5f);
            Color color0 = Color.Lerp(colorNeutral, color1, 0.20f);
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
            dataScrollablePanel.size = new Vector2(onPanel.size.x, onPanel.size.y - HeightOfTotals);
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
                // the table below shows the ticks/day and days per full updates at the 3 different simulation speeds of the base game
                // some mods increase the simulation speed, which reduces the ticks/day, which increases the days per full update
                // the game speed and city population do not change the number of ticks per real time
                // sim speed 1 = 585 ticks/day = 0.22 days/update
                // sim speed 2 = 293 ticks/day = 0.44 days/update
                // sim speed 3 = 145 ticks/day = 0.88 days/update
                uint bufferSize = (uint)CitizenManager.instance.m_citizens.m_buffer.Length;
                uint lastCitizen = Math.Min(_citizenCounter + 8192, bufferSize);
                for (; _citizenCounter < lastCitizen; _citizenCounter++)
                {
                    // get the demographic data for the citizen in the temp buffer
                    CitizenDemographic citizenDemographic = GetCitizenDemographicData(_citizenCounter);
                    if (citizenDemographic != null)
                    {
                        _tempCitizens.Add(citizenDemographic);
                    }
                }

                // check for completed all groups
                if (_citizenCounter >= bufferSize)
                {
                    try
                    {
                        // lock thread while working with final buffer
                        LockThread();

                        // copy temp to final (final is used by the UI)
                        _finalCitizens = _tempCitizens;
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

                    // start over on next simulation tick
                    _tempCitizens = new CitizenDemographics();
                    _citizenCounter = 0;
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogException(ex);
            }
        }

        /// <summary>
        /// get the demographic data for the specified citizen
        /// return null for citizen that should not be included
        /// </summary>
        private CitizenDemographic GetCitizenDemographicData(uint citizenID)
        {
            // citizen must be created and must not be a tourist
            Citizen citizen = CitizenManager.instance.m_citizens.m_buffer[citizenID];
            if ((citizen.m_flags & Citizen.Flags.Created) != 0 && (citizen.m_flags & Citizen.Flags.Tourist) == 0)
            {
                // citizen must have a home building
                if (citizen.m_homeBuilding != 0)
                {
                    // home building must have an AI
                    Building homeBuilding = BuildingManager.instance.m_buildings.m_buffer[citizen.m_homeBuilding];
                    if (homeBuilding.Info != null && homeBuilding.Info.m_buildingAI != null)
                    {
                        // loop over type hierarchy of home building
                        Type homeBuildingAIType = homeBuilding.Info.m_buildingAI.GetType();
                        while (homeBuildingAIType != null)
                        {
                            // building AI must be or derive from ResidentialBuildingAI   OR   building AI must be or derive from NursingHomeAi (mod)
                            // PloppableRICO.GrowableResidentialAI and PloppableRICO.PloppableResidentialAI derive from ResidentialBuildingAI
                            if (homeBuildingAIType == typeof(ResidentialBuildingAI) || homeBuildingAIType.Name == "NursingHomeAi")
                            {
                                // set citizen demographic data
                                CitizenDemographic citizenDemographic = new CitizenDemographic();
                                citizenDemographic.citizenID   = citizenID;
                                citizenDemographic.districtID  = DistrictManager.instance.GetDistrict(homeBuilding.m_position);
                                citizenDemographic.deceasesd   = ((citizen.m_flags & Citizen.Flags.Dead) != 0);
                                citizenDemographic.movingIn    = ((citizen.m_flags & Citizen.Flags.MovingIn) != 0);
                                citizenDemographic.age         = Mathf.Clamp((int)(citizen.Age * RealAgePerGameAge), 0, MaxRealAge);
                                citizenDemographic.ageGroup    = Citizen.GetAgeGroup(citizen.Age);
                                citizenDemographic.education   = citizen.EducationLevel;
                                citizenDemographic.employment  = ((citizen.m_flags & Citizen.Flags.Student) != 0 ? EmploymentStatus.Student :
                                                                  (citizen.m_workBuilding != 0 ? EmploymentStatus.Employed : EmploymentStatus.Unemployed));
                                citizenDemographic.gender      = Citizen.GetGender(citizenID);
                                citizenDemographic.happiness   = Citizen.GetHappinessLevel(Citizen.GetHappiness(citizen.m_health, citizen.m_wellbeing));
                                citizenDemographic.health      = Citizen.GetHealthLevel(citizen.m_health);
                                citizenDemographic.location    = citizen.CurrentLocation;
                                citizenDemographic.residential = homeBuilding.Info.m_class.m_level;
                                citizenDemographic.student     = citizen.GetCurrentSchoolLevel(citizenID);
                                citizenDemographic.wealth      = citizen.WealthLevel;
                                citizenDemographic.wellbeing   = Citizen.GetWellbeingLevel(citizen.EducationLevel, citizen.m_wellbeing);

                                // return citizen demographic data
                                return citizenDemographic;
                            }

                            // continue with base building AI type
                            homeBuildingAIType = homeBuildingAIType.BaseType;
                        }
                    }
                }
            }

            // citizen should not be included
            return null;
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
        /// Update is called every frame
        /// </summary>
        public override void Update()
        {
            // do base processing
            base.Update();

            try
            {
                // only update the panel when it is triggered for update
                if (_triggerUpdatePanel)
                {
                    // get row and column selections from config
                    Configuration config = ConfigurationUtil<Configuration>.Load();
                    RowSelection    rowSelection    = (RowSelection   )config.RowSelection;
                    ColumnSelection columnSelection = (ColumnSelection)config.ColumnSelection;

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
                        _dataPanel.height = selectedRowCount * TextHeight + HeightOfTotals;
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
                                else if (citizen.deceasesd)
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

        #endregion
    }
}
