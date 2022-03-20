using ColossalFramework.UI;
using ColossalFramework.Globalization;
using UnityEngine;
using System;

namespace PopulationDemographics
{
    /// <summary>
    /// a panel to display things on the screen
    /// </summary>
    public class PopulationDemographicsPanel : UIPanel
    {
        // age constants
        private const int MaxGameAge = 400;                     // obtained from Citizen.Age
        private const float RealAgePerGameAge = 1f / 3.5f;      // obtained from District.GetAverageLifespan
        private const int MaxRealAge = (int)(MaxGameAge * RealAgePerGameAge);

        // text
        UIFont _textFont = null;
        private readonly Color32 DataTextColor = new Color32(185, 221, 254, 255);
        private readonly Color32 HeadingTextColor = new Color32(206, 248, 0, 255);

        // common count attributes
        private const float CountWidth = 67f;
        private const float TextHeight = 15f;
        private const float TextHeightAge = 9f;
        private const float LabelSpacing = 4f;

        // one data row by education level
        private class DataRow
        {
            public int eduLevel0;
            public int eduLevel1;
            public int eduLevel2;
            public int eduLevel3;
            public int movingIn;
            public int deceased;

            public int total
            {
                get { return eduLevel0 + eduLevel1 + eduLevel2 + eduLevel3; }
            }

            public void Reset()
            {
                // reset each element
                eduLevel0 = 0;
                eduLevel1 = 0;
                eduLevel2 = 0;
                eduLevel3 = 0;
                movingIn = 0;
                deceased = 0;
            }

            public void Copy(DataRow value)
            {
                // copy each element
                eduLevel0 = value.eduLevel0;
                eduLevel1 = value.eduLevel1;
                eduLevel2 = value.eduLevel2;
                eduLevel3 = value.eduLevel3;
                movingIn = value.movingIn;
                deceased = value.deceased;
            }
        }

        // the data rows by age group and age
        private class DataRows
        {
            public DataRow children = new DataRow();
            public DataRow teens    = new DataRow();
            public DataRow youngs   = new DataRow();
            public DataRow adults   = new DataRow();
            public DataRow seniors  = new DataRow();
            public DataRow[] age    = new DataRow[MaxRealAge + 1];
            public DataRow total    = new DataRow();
            public DataRow movingIn = new DataRow();
            public DataRow deceased = new DataRow();

            public DataRows()
            {
                // initialize age array
                for (int i = 0; i < age.Length; i++)
                {
                    age[i] = new DataRow();
                }
            }

            public void Reset()
            {
                // reset each data row
                children.Reset();
                teens.Reset();
                youngs.Reset();
                adults.Reset();
                seniors.Reset();
                for (int i = 0; i < age.Length; i++)
                {
                    age[i].Reset();
                }
                total.Reset();
                movingIn.Reset();
                deceased.Reset();
            }

            public void Copy(DataRows value)
            {
                // copy each data row
                children.Copy(value.children);
                teens.Copy(value.teens);
                youngs.Copy(value.youngs);
                adults.Copy(value.adults);
                seniors.Copy(value.seniors);
                for (int i = 0; i < age.Length; i++)
                {
                    age[i].Copy(value.age[i]);
                }
                movingIn.Copy(value.movingIn);
                deceased.Copy(value.deceased);

                // compute totals for the total data row
                total.eduLevel0 = children.eduLevel0 + teens.eduLevel0 + youngs.eduLevel0 + adults.eduLevel0 + seniors.eduLevel0;
                total.eduLevel1 = children.eduLevel1 + teens.eduLevel1 + youngs.eduLevel1 + adults.eduLevel1 + seniors.eduLevel1;
                total.eduLevel2 = children.eduLevel2 + teens.eduLevel2 + youngs.eduLevel2 + adults.eduLevel2 + seniors.eduLevel2;
                total.eduLevel3 = children.eduLevel3 + teens.eduLevel3 + youngs.eduLevel3 + adults.eduLevel3 + seniors.eduLevel3;
                total.movingIn  = children.movingIn  + teens.movingIn  + youngs.movingIn  + adults.movingIn  + seniors.movingIn;
                total.deceased  = children.deceased  + teens.deceased  + youngs.deceased  + adults.deceased  + seniors.deceased;
            }
        }

        // temp and final counts
        private DataRows _tempCount = new DataRows();
        private DataRows _finalCount = new DataRows();

        // UI elements for one data row
        private class DataRowUI
        {
            public UILabel description;
            public UISprite amountBar;
            public UILabel eduLevel0;
            public UILabel eduLevel1;
            public UILabel eduLevel2;
            public UILabel eduLevel3;
            public UILabel total;
            public UILabel movingIn;
            public UILabel deceased;
        }

        // panels to hold groups of data rows
        private UIPanel _ageGroupDataPanel;
        private UIScrollablePanel _ageDataScrollablePanel;
        private UIPanel _ageDataPanel;
        private UIPanel _totalDataPanel;

        // UI elements for each data row
        private DataRowUI _heading;
        private DataRowUI _children;
        private DataRowUI _teens;
        private DataRowUI _youngs;
        private DataRowUI _adults;
        private DataRowUI _seniors;
        private DataRowUI[] _age = new DataRowUI[MaxRealAge + 1];
        private DataRowUI _total;
        private DataRowUI _movingIn;
        private DataRowUI _deceased;

        // UI elements for age group and age options
        private UIPanel _ageGroupOptionPanel;
        private UIPanel _ageOptionPanel;
        private UISprite _ageGroupCheckBox;
        private UISprite _ageCheckBox;

        // UI elements for count/percent buttons
        private UIPanel _countPanel;
        private UIPanel _percentPanel;
        private UISprite _countCheckBox;
        private UISprite _percentCheckBox;

        // other UI elements
        private UIButton _closeButton;
        public const float ScrollbarWidth = 16f;

        // miscellaneous
        private bool _triggerUpdatePanel = false;
        private int _districtCounter = 0;

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
                height = 300;

                // set initial visibility from config
                Configuration config = ConfigurationUtil<Configuration>.Load();
                isVisible = config.PanelVisible;

                // get the PopulationInfoViewPanel panel (displayed when the user clicks on the Population info view button)
                PopulationInfoViewPanel populationPanel = UIView.library.Get<PopulationInfoViewPanel>(typeof(PopulationInfoViewPanel).Name);
                if (populationPanel == null)
                {
                    LogUtil.LogError("Unable to find PopulationInfoViewPanel.");
                    return;
                }

                // place panel to the right of PopulationInfoViewPanel
                relativePosition = new Vector3(populationPanel.component.size.x - 1f, 0f);

                // copy text font from the Population label
                UILabel populationLabel = populationPanel.Find<UILabel>("Population");
                if (populationLabel == null)
                {
                    LogUtil.LogError("Unable to find Population label on PopulationInfoViewPanel.");
                    return;
                }
                _textFont = populationLabel.font;

                // create heading row
                float headingTop = 55f;
                if (!CreateDataRow(this, headingTop, "Heading", "", 0.75f, TextHeight, Color.black, out _heading)) return;

                // adjust heading properties
                _heading.eduLevel0.text = "Unedu";
                _heading.eduLevel1.text = "Educated";
                _heading.eduLevel2.text = "Well Edu";
                _heading.eduLevel3.text = "High Edu";
                _heading.total    .text = "Total";
                _heading.deceased .text = "Deceased";
                _heading.movingIn .text = "MovingIn";

                _heading.eduLevel0.tooltip = "Uneducated - Elementary School not completed";
                _heading.eduLevel1.tooltip = "Educated - completed Elementary School";
                _heading.eduLevel2.tooltip = "Well Educated - completed High School";
                _heading.eduLevel3.tooltip = "Highly Educated - completed University";

                _heading.eduLevel0.textColor =
                _heading.eduLevel1.textColor =
                _heading.eduLevel2.textColor =
                _heading.eduLevel3.textColor =
                _heading.total    .textColor =
                _heading.deceased .textColor =
                _heading.movingIn .textColor = HeadingTextColor;

                _heading.amountBar.isVisible = false;

                // set panel width according to headings and allow additional width for age scroll bar
                width = _heading.description.relativePosition.x + _heading.deceased.relativePosition.x + _heading.deceased.size.x + 20f;

                // create lines after headings
                headingTop += 15f;
                CreateLines(this, headingTop, "Heading");

                // create age group and age option panels
                CreateAgeGroupAgePanel(this, _heading.description.relativePosition.x, 40f,              "AgeGroupOption", "Age Group", out _ageGroupOptionPanel, out _ageGroupCheckBox);
                CreateAgeGroupAgePanel(this, _heading.description.relativePosition.x, 40f + TextHeight, "AgeOption",      "Age",       out _ageOptionPanel,      out _ageCheckBox);

                // create the title label
                UILabel title = AddUIComponent<UILabel>();
                if (title == null)
                {
                    LogUtil.LogError($"Unable to create title label on panel [{name}].");
                    return;
                }
                title.name = "Title";
                title.font = _textFont;
                title.text = "Demographics";
                title.textAlignment = UIHorizontalAlignment.Center;
                title.textScale = 1f;
                title.textColor = new Color32(254, 254, 254, 255);
                title.autoSize = false;
                title.size = new Vector2(width, 18f);
                title.relativePosition = new Vector3(0f, 11f);
                title.isVisible = true;

                // create population icon in upper left
                UISprite panelIcon = AddUIComponent<UISprite>();
                if (panelIcon == null)
                {
                    LogUtil.LogError($"Unable to create population icon on panel [{name}].");
                    return;
                }
                panelIcon.name = "PopulationIcon";
                panelIcon.autoSize = false;
                panelIcon.size = new Vector2(36f, 36f);
                panelIcon.relativePosition = new Vector3(10f, 2f);
                panelIcon.spriteName = "InfoIconPopulationPressed";
                panelIcon.isVisible = true;

                // create close button
                _closeButton = AddUIComponent<UIButton>();
                if (_closeButton == null)
                {
                    LogUtil.LogError($"Unable to create close button on panel [{name}].");
                    return;
                }
                _closeButton.name = "CloseButton";
                _closeButton.autoSize = false;
                _closeButton.size = new Vector2(32f, 32f);
                _closeButton.relativePosition = new Vector3(width - 34f, 2f);
                _closeButton.normalBgSprite = "buttonclose";
                _closeButton.hoveredBgSprite = "buttonclosehover";
                _closeButton.pressedBgSprite = "buttonclosepressed";
                _closeButton.isVisible = true;
                _closeButton.eventClicked += CloseButton_eventClicked;

                // compute age group colors, which are slightly darker than the colors from the Population Info View panel
                const float colorMultiplier = 0.7f;
                Color32 colorChild  = (Color)populationPanel.m_ChildColor  * colorMultiplier;
                Color32 colorTeen   = (Color)populationPanel.m_TeenColor   * colorMultiplier;
                Color32 colorYoung  = (Color)populationPanel.m_YoungColor  * colorMultiplier;
                Color32 colorAdult  = (Color)populationPanel.m_AdultColor  * colorMultiplier;
                Color32 colorSenior = (Color)populationPanel.m_SeniorColor * colorMultiplier;

                // create panel to hold age group data rows
                float agePanelsTop = headingTop + 4f;
                _ageGroupDataPanel = AddUIComponent<UIPanel>();
                if (_ageGroupDataPanel == null)
                {
                    LogUtil.LogError($"Unable to create age group data panel on panel [{name}].");
                    return;
                }

                // create data rows by age group
                float ageGroupTop = 0f;
                if (!CreateDataRow(_ageGroupDataPanel, ageGroupTop, "Children", "Children",     0.875f, TextHeight, colorChild,  out _children)) return; ageGroupTop += TextHeight;
                if (!CreateDataRow(_ageGroupDataPanel, ageGroupTop, "Teens",    "Teens",        0.875f, TextHeight, colorTeen,   out _teens   )) return; ageGroupTop += TextHeight;
                if (!CreateDataRow(_ageGroupDataPanel, ageGroupTop, "Young",    "Young Adults", 0.875f, TextHeight, colorYoung,  out _youngs  )) return; ageGroupTop += TextHeight;
                if (!CreateDataRow(_ageGroupDataPanel, ageGroupTop, "Adults",   "Adults",       0.875f, TextHeight, colorAdult,  out _adults  )) return; ageGroupTop += TextHeight;
                if (!CreateDataRow(_ageGroupDataPanel, ageGroupTop, "Seniors",  "Seniors",      0.875f, TextHeight, colorSenior, out _seniors )) return; ageGroupTop += TextHeight;

                // finish age group data panel
                _ageGroupDataPanel.name = "AgeGroupDataPanel";
                _ageGroupDataPanel.autoSize = false;
                _ageGroupDataPanel.size = new Vector2(width, _seniors.description.relativePosition.y + _seniors.description.size.y);
                _ageGroupDataPanel.relativePosition = new Vector3(0f, agePanelsTop);

                // create scrollable panel to hold age data panel
                if (!CreateAgeDataScrollablePanel(out _ageDataScrollablePanel)) return;

                // create panel to hold age data rows
                _ageDataPanel = _ageDataScrollablePanel.AddUIComponent<UIPanel>();
                if (_ageDataPanel == null)
                {
                    LogUtil.LogError($"Unable to create age data panel on panel [{name}].");
                    return;
                }

                // do each age
                float ageTop = 0;
                for (int i = 0; i < _age.Length; i++)
                {
                    // compute color
                    Color32 color = Color.black;
                    switch(Citizen.GetAgeGroup((int)(i / RealAgePerGameAge)))
                    {
                        case Citizen.AgeGroup.Child:  color = colorChild;  break;
                        case Citizen.AgeGroup.Teen:   color = colorTeen;   break;
                        case Citizen.AgeGroup.Young:  color = colorYoung;  break;
                        case Citizen.AgeGroup.Adult:  color = colorAdult;  break;
                        case Citizen.AgeGroup.Senior: color = colorSenior; break;
                    }

                    // create the data row
                    if (!CreateDataRow(_ageDataPanel, ageTop, "Age" + i, i.ToString(), 0.625f, TextHeightAge, color, out _age[i])) return; ageTop += TextHeightAge;
                }

                // finish age data panel
                _ageDataPanel.name = "AgeDataPanel";
                _ageDataPanel.autoSize = false;
                _ageDataPanel.size = new Vector2(width, _age[_age.Length - 1].description.relativePosition.y + _age[_age.Length - 1].description.size.y);
                _ageDataPanel.relativePosition = new Vector3(0f, 0f);

                // finish age data scrollable panel
                _ageDataScrollablePanel.name = "AgeDataScrollablePanel";
                _ageDataScrollablePanel.autoSize = false;
                _ageDataScrollablePanel.size = new Vector2(width, 780f);
                _ageDataScrollablePanel.relativePosition = new Vector3(0f, agePanelsTop);

                _ageDataScrollablePanel.verticalScrollbar.autoSize = false;
                _ageDataScrollablePanel.verticalScrollbar.size = new Vector3(_ageDataScrollablePanel.verticalScrollbar.size.x, _ageDataScrollablePanel.size.y);
                _ageDataScrollablePanel.verticalScrollbar.relativePosition = new Vector3(_ageDataScrollablePanel.verticalScrollbar.relativePosition.x, agePanelsTop);
                _ageDataScrollablePanel.verticalScrollbar.trackObject.size = new Vector2(ScrollbarWidth, _ageDataScrollablePanel.size.y);

                // create panel to hold totals
                _totalDataPanel = AddUIComponent<UIPanel>();
                if (_totalDataPanel == null)
                {
                    LogUtil.LogError($"Unable to create total data panel on panel [{name}].");
                    return;
                }

                // create total data row
                float totalTop = 0;
                CreateLines(_totalDataPanel, totalTop, "Totals");
                totalTop += 4f;
                if (!CreateDataRow(_totalDataPanel, totalTop, "Total", "Total", 0.875f, TextHeight, Color.black, out _total)) return; totalTop += TextHeight;
                _total.amountBar.isVisible = false;

                // create other data rows
                totalTop += 12f;
                if (!CreateDataRow(_totalDataPanel, totalTop, "MovingIn", "Moving In", 0.875f, TextHeight, Color.black, out _movingIn)) return; totalTop += TextHeight;
                if (!CreateDataRow(_totalDataPanel, totalTop, "Deceased", "Deceased",  0.875f, TextHeight, Color.black, out _deceased)) return; totalTop += TextHeight;

                // hide amount bars
                _movingIn.amountBar.isVisible = false;
                _deceased.amountBar.isVisible = false;

                // hide duplicates for moving in and deceased
                _movingIn.movingIn.isVisible = false;
                _movingIn.deceased.isVisible = false;
                _deceased.movingIn.isVisible = false;
                _deceased.deceased.isVisible = false;

                // create count/percent panels
                CreateCountPercentPanel(_totalDataPanel, _movingIn.description.relativePosition.y, "CountOption",   "Count",   out _countPanel,   out _countCheckBox);
                CreateCountPercentPanel(_totalDataPanel, _deceased.description.relativePosition.y, "PercentOption", "Percent", out _percentPanel, out _percentCheckBox);

                // set initial count or percent from config
                SetCheckBox(config.CountStatus ? _countCheckBox : _percentCheckBox, true);

                // finish total data panel
                _totalDataPanel.name = "TotalDataPanel";
                _totalDataPanel.autoSize = false;
                _totalDataPanel.size = new Vector2(width, _deceased.description.relativePosition.y + _deceased.description.size.y);
                _totalDataPanel.relativePosition = new Vector3(0f, totalTop);

                // set initial age group or age as if user clicked on it
                AgeOption_eventClicked(config.AgeGroupStatus ? _ageGroupOptionPanel : _ageOptionPanel, null);

                // make sure manager exists
                if (!BuildingManager.exists)
                {
                    LogUtil.LogError($"BuildingManager not ready during panel initialization.");
                    return;
                }

                // initialize population counts, do each building
                Building[] buffer = BuildingManager.instance.m_buildings.m_buffer;
                for (ushort buildingID = 1; buildingID < buffer.Length; buildingID++)
                {
                    // do only buildings that have a valid index
                    if (buffer[buildingID].m_infoIndex != 0)
                    {
                        // do only buildings with an AI
                        if (buffer[buildingID].Info != null && buffer[buildingID].Info.m_buildingAI != null)
                        {
                            // loop over building type hierarchy
                            Type type = buffer[buildingID].Info.m_buildingAI.GetType();
                            while (type != null)
                            {
                                // check if building AI is or derives from ResidentialBuildingAI
                                // PloppableRICO.GrowableResidentialAI and PloppableRICO.PloppableResidentialAI derive from ResidentialBuildingAI
                                if (type == typeof(ResidentialBuildingAI))
                                {
                                    ResidentialSimulationStepActive(buildingID, ref buffer[buildingID], false);
                                    break;
                                }

                                // check if building AI is or derives from NursingHomeAi (mod)
                                if (type.Name == "NursingHomeAi")
                                {
                                    // do same logic as for ResidentialBuildingAI
                                    ResidentialSimulationStepActive(buildingID, ref buffer[buildingID], false);
                                    break;
                                }

                                // continue with base type
                                type = type.BaseType;
                            }
                        }
                    }
                }

                // now do logic for District.SimulationStep
                DistrictSimulationStep(0, false);
            }
            catch (Exception ex)
            {
                LogUtil.LogException(ex);
            }
        }

        /// <summary>
        /// handle Close button clicked
        /// </summary>
        private void CloseButton_eventClicked(UIComponent component, UIMouseEventParameter eventParam)
        {
            // hide this panel
            isVisible = false;
            Configuration.SavePanelVisible(isVisible);
        }

        /// <summary>
        /// create a UI data row
        /// </summary>
        /// <param name="top">top position</param>
        /// <param name="namePrefix">component name prefix</param>
        /// <param name="text">description text</param>
        /// <param name="dataRow">ouitput the UI data row</param>
        /// <returns>success status</returns>
        private bool CreateDataRow(UIPanel onPanel, float top, string namePrefix, string text, float textScale, float height, Color32 barColor, out DataRowUI dataRow)
        {
            // create new worker data
            dataRow = new DataRowUI();

            // create label for description
            dataRow.description = onPanel.AddUIComponent<UILabel>();
            if (dataRow.description == null)
            {
                LogUtil.LogError($"Unable to create description label for [{namePrefix}] on panel [{name}].");
                return false;
            }
            dataRow.description.name = namePrefix + "Description";
            dataRow.description.font = _textFont;
            dataRow.description.text = text;
            dataRow.description.textAlignment = UIHorizontalAlignment.Left;
            dataRow.description.verticalAlignment = UIVerticalAlignment.Bottom;
            dataRow.description.textScale = textScale;
            dataRow.description.textColor = DataTextColor;
            dataRow.description.autoSize = false;
            dataRow.description.size = new Vector2(100f, height);
            dataRow.description.relativePosition = new Vector3(8f, top);
            dataRow.description.isVisible = true;

            // create amount bar
            dataRow.amountBar = onPanel.AddUIComponent<UISprite>();
            if (dataRow.amountBar == null)
            {
                LogUtil.LogError($"Unable to create age amount bar for [{namePrefix}] on panel [{name}].");
                return false;
            }
            dataRow.amountBar.name = namePrefix + "AmountBar";
            dataRow.amountBar.relativePosition = new Vector3(dataRow.description.relativePosition.x, dataRow.description.relativePosition.y);
            dataRow.amountBar.spriteName = "EmptySprite";
            dataRow.amountBar.autoSize = false;
            dataRow.amountBar.size = new Vector2(dataRow.description.size.x, dataRow.description.size.y - 1f);
            dataRow.amountBar.color = barColor;
            dataRow.amountBar.fillDirection = UIFillDirection.Horizontal;
            dataRow.amountBar.isVisible = true;
            dataRow.amountBar.SendToBack();

            // create count labels
            if (!CreateCountLabel(onPanel, 0f,  top, namePrefix + "Level0",   textScale, height, dataRow.description, out dataRow.eduLevel0)) return false;
            if (!CreateCountLabel(onPanel, 0f,  top, namePrefix + "Level1",   textScale, height, dataRow.eduLevel0,   out dataRow.eduLevel1)) return false;
            if (!CreateCountLabel(onPanel, 0f,  top, namePrefix + "Level2",   textScale, height, dataRow.eduLevel1,   out dataRow.eduLevel2)) return false;
            if (!CreateCountLabel(onPanel, 0f,  top, namePrefix + "Level3",   textScale, height, dataRow.eduLevel2,   out dataRow.eduLevel3)) return false;
            if (!CreateCountLabel(onPanel, 0f,  top, namePrefix + "Total",    textScale, height, dataRow.eduLevel3,   out dataRow.total    )) return false;

            if (!CreateCountLabel(onPanel, 12f, top, namePrefix + "MovingIn", textScale, height, dataRow.total,       out dataRow.movingIn )) return false;
            if (!CreateCountLabel(onPanel, 0f,  top, namePrefix + "Deceased", textScale, height, dataRow.movingIn,    out dataRow.deceased )) return false;

            // success
            return true;
        }

        /// <summary>
        /// create a label that displays a count
        /// </summary>
        /// <param name="leftAdd">amount to add to the default left position</param>
        /// <param name="top">top position</param>
        /// <param name="labelName">name of the count label</param>
        /// <param name="previousLabel">the previous label</param>
        /// <param name="count">output the count label</param>
        /// <returns>success status</returns>
        private bool CreateCountLabel(UIPanel onPanel, float leftAdd, float top, string labelName, float textScale, float height, UILabel previousLabel, out UILabel count)
        {
            count = onPanel.AddUIComponent<UILabel>();
            if (count == null)
            {
                LogUtil.LogError($"Unable to create label [{labelName}] on panel [{name}].");
                return false;
            }
            count.name = labelName;
            count.font = _textFont;
            count.text = "000,000";
            count.textAlignment = UIHorizontalAlignment.Right;
            count.verticalAlignment = UIVerticalAlignment.Bottom;
            count.textScale = textScale;
            count.textColor = DataTextColor;
            count.autoSize = false;
            count.size = new Vector2(CountWidth, height);
            count.relativePosition = new Vector3(previousLabel.relativePosition.x + previousLabel.size.x + LabelSpacing + leftAdd, top);
            count.isVisible = true;

            // success
            return true;
        }

        /// <summary>
        /// create UI lines
        /// </summary>
        /// <param name="top">top position</param>
        /// <param name="namePrefix">component name prefix</param>
        /// <returns>success status</returns>
        private bool CreateLines(UIPanel onPanel, float top, string namePrefix)
        {
            // compute line color
            const float ColorMult = 0.8f;
            Color32 lineColor = new Color32((byte)(HeadingTextColor.r * ColorMult), (byte)(HeadingTextColor.g * ColorMult), (byte)(HeadingTextColor.b * ColorMult), 255);

            // a line is needed for each column except description
            for (int i = 0; i < 7; ++i)
            {
                UISprite line = onPanel.AddUIComponent<UISprite>();
                if (line == null)
                {
                    LogUtil.LogError($"Unable to create [{namePrefix}] line sprite [{i}] on panel [{name}].");
                    return false;
                }
                line.name = namePrefix + "Line" + i.ToString();
                line.autoSize = false;
                line.size = new Vector2(CountWidth, 2f);
                float lineLeft =
                    (i == 0 ? _heading.eduLevel0.relativePosition.x :
                    (i == 1 ? _heading.eduLevel1.relativePosition.x :
                    (i == 2 ? _heading.eduLevel2.relativePosition.x :
                    (i == 3 ? _heading.eduLevel3.relativePosition.x :
                    (i == 4 ? _heading.total.relativePosition.x :
                    (i == 5 ? _heading.movingIn.relativePosition.x :
                              _heading.deceased.relativePosition.x))))));
                line.relativePosition = new Vector3(lineLeft + 2f, top);
                line.spriteName = "EmptySprite";
                line.color = lineColor;
                line.isVisible = true;
            }

            // success
            return true;
        }

        /// <summary>
        /// create the age data scrollable panel on which the age data panel will be created
        /// </summary>
        private bool CreateAgeDataScrollablePanel(out UIScrollablePanel ageDataScrollablePanel)
        {
            // create scrollable panel
            ageDataScrollablePanel = AddUIComponent<UIScrollablePanel>();
            if (ageDataScrollablePanel == null)
            {
                LogUtil.LogError($"Unable to create age data scrollable panel on panel {name}.");
                return false;
            }
            ageDataScrollablePanel.name = "ageDataScrollablePanel";
            ageDataScrollablePanel.backgroundSprite = string.Empty;
            ageDataScrollablePanel.clipChildren = true;      // prevents contained components from being displayed when they are scrolled out of view
            ageDataScrollablePanel.autoLayoutStart = LayoutStart.TopLeft;
            ageDataScrollablePanel.autoLayoutDirection = LayoutDirection.Vertical;
            ageDataScrollablePanel.autoLayout = true;
            ageDataScrollablePanel.scrollWheelDirection = UIOrientation.Vertical;
            ageDataScrollablePanel.builtinKeyNavigation = true;
            ageDataScrollablePanel.scrollWithArrowKeys = true;

            // create vertical scroll bar
            UIScrollbar verticalScrollbar = AddUIComponent<UIScrollbar>();
            if (verticalScrollbar == null)
            {
                LogUtil.LogError($"Unable to create age scrollbar.");
                return false;
            }
            verticalScrollbar.name = "VerticalScrollbar";
            verticalScrollbar.relativePosition = new Vector2(width - ScrollbarWidth, 0f);
            verticalScrollbar.orientation = UIOrientation.Vertical;
            verticalScrollbar.stepSize = 10f;
            verticalScrollbar.incrementAmount = 50f;
            verticalScrollbar.scrollEasingType = ColossalFramework.EasingType.BackEaseOut;
            ageDataScrollablePanel.verticalScrollbar = verticalScrollbar;

            // create scroll bar track on scroll bar
            UISlicedSprite verticalScrollbarTrack = verticalScrollbar.AddUIComponent<UISlicedSprite>();
            if (verticalScrollbarTrack == null)
            {
                LogUtil.LogError($"Unable to create age scrollbar track.");
                return false;
            }
            verticalScrollbarTrack.name = "VerticalScrollbarTrack";
            verticalScrollbarTrack.relativePosition = Vector3.zero;
            verticalScrollbarTrack.spriteName = "ScrollbarTrack";
            verticalScrollbar.trackObject = verticalScrollbarTrack;

            // create scroll bar thumb on scroll bar track
            UISlicedSprite verticalScrollbarThumb = verticalScrollbarTrack.AddUIComponent<UISlicedSprite>();
            if (verticalScrollbarThumb == null)
            {
                LogUtil.LogError($"Unable to create age scrollbar thumb.");
                return false;
            }
            verticalScrollbarThumb.name = "VerticalScrollbarThumb";
            verticalScrollbarThumb.autoSize = true;
            verticalScrollbarThumb.size = new Vector2(ScrollbarWidth - 4f, 0f);
            verticalScrollbarThumb.relativePosition = Vector3.zero;
            verticalScrollbarThumb.spriteName = "ScrollbarThumb";
            verticalScrollbar.thumbObject = verticalScrollbarThumb;

            // success
            return true;
        }

        /// <summary>
        /// create a panel to hold a age group vs age check box and label
        /// </summary>
        /// <param name="left">left position of the panel</param>
        /// <param name="top">top position of the panel</param>
        /// <param name="namePrefix">component name prefix</param>
        /// <param name="labelText">text for the label</param>
        /// <param name="panel">output the panel</param>
        /// <param name="checkBox">output the check box</param>
        private bool CreateAgeGroupAgePanel(UIPanel onPanel, float left, float top, string namePrefix, string labelText, out UIPanel panel, out UISprite checkBox)
        {
            // satisfy compiler
            panel = null;
            checkBox = null;

            // create a new panel
            panel = onPanel.AddUIComponent<UIPanel>();
            if (panel == null)
            {
                LogUtil.LogError($"Unable to create panel [{namePrefix}] on panel [{name}].");
                return false;
            }
            panel.name = namePrefix + "Panel";
            panel.size = new Vector2(90f, TextHeight);
            panel.relativePosition = new Vector3(left, top);
            panel.isVisible = true;

            // set up click event handler
            // a click on any contained component triggers a click event on the panel
            // therefore, each individual component does not need its own click event handler
            panel.eventClicked += AgeOption_eventClicked;

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
            description.font = _textFont;
            description.text = labelText;
            description.textAlignment = UIHorizontalAlignment.Left;
            description.verticalAlignment = UIVerticalAlignment.Bottom;
            description.textScale = 0.75f;
            description.textColor = DataTextColor;
            description.autoSize = false;
            description.size = new Vector2(panel.width - checkBox.size.x - 5f, TextHeight);
            description.relativePosition = new Vector3(checkBox.size.x + 5f, 2f);
            description.isVisible = true;

            // success
            return true;
        }

        /// <summary>
        /// create a panel to hold a count vs percent check box and label
        /// </summary>
        /// <param name="top">top position of the panel</param>
        /// <param name="namePrefix">component name prefix</param>
        /// <param name="labelText">text for the label</param>
        /// <param name="panel">output the panel</param>
        /// <param name="checkBox">output the check box</param>
        private bool CreateCountPercentPanel(UIPanel onPanel, float top, string namePrefix, string labelText, out UIPanel panel, out UISprite checkBox)
        {
            // satisfy compiler
            panel = null;
            checkBox = null;

            // create a new panel
            panel = onPanel.AddUIComponent<UIPanel>();
            if (panel == null)
            {
                LogUtil.LogError($"Unable to create panel [{namePrefix}] on panel [{name}].");
                return false;
            }
            panel.name = namePrefix + "Panel";
            panel.size = new Vector2(90f, TextHeight);
            panel.relativePosition = new Vector3(width - panel.size.x - ScrollbarWidth - 10f, top);
            panel.isVisible = true;

            // set up click event handler
            // a click on any contained component triggers a click event on the panel
            // therefore, each individual component does not need its own click event handler
            panel.eventClicked += DisplayOption_eventClicked;

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
            description.font = _textFont;
            description.text = labelText;
            description.textAlignment = UIHorizontalAlignment.Left;
            description.verticalAlignment = UIVerticalAlignment.Bottom;
            description.textScale = 0.875f;
            description.textColor = DataTextColor;
            description.autoSize = false;
            description.size = new Vector2(panel.width - checkBox.size.x - 5f, TextHeight);
            description.relativePosition = new Vector3(checkBox.size.x + 5f, 2f);
            description.isVisible = true;

            // success
            return true;
        }

        /// <summary>
        /// Clicked event handler for age options
        /// </summary>
        /// <param name="component"></param>
        /// <param name="eventParam"></param>
        private void AgeOption_eventClicked(UIComponent component, UIMouseEventParameter eventParam)
        {
            try
            {
                // determine which whether or not age group option was clicked
                bool ageGroupOption = (component == _ageGroupOptionPanel);

                // set check box that was clicked and clear the other check box
                SetCheckBox(_ageGroupCheckBox, ageGroupOption);
                SetCheckBox(_ageCheckBox,      !ageGroupOption);

                // save age selection status to config
                Configuration.SaveAgeGroupStatus(IsCheckBoxChecked(_ageGroupCheckBox));

                // show selected data panel and hide the other data panel
                _ageGroupDataPanel.isVisible                        = ageGroupOption;
                _ageDataScrollablePanel.isVisible                   = !ageGroupOption;
                _ageDataScrollablePanel.verticalScrollbar.isVisible = !ageGroupOption;

                // adjust position of total data panel according to which check box was clicked
                _totalDataPanel.relativePosition = new Vector3(0f, ageGroupOption ? _ageGroupDataPanel.relativePosition.y + _ageGroupDataPanel.size.y : _ageDataScrollablePanel.relativePosition.y + _ageDataScrollablePanel.size.y);

                // adjust panel height
                height = _totalDataPanel.relativePosition.y + _totalDataPanel.size.y + 5f;

                // trigger the panel to update
                _triggerUpdatePanel = true;
            }
            catch (Exception ex)
            {
                LogUtil.LogException(ex);
            }
        }

        /// <summary>
        /// Clicked event handler for display options
        /// </summary>
        /// <param name="component">the component clicked</param>
        /// <param name="eventParam">event parameters</param>
        private void DisplayOption_eventClicked(UIComponent component, UIMouseEventParameter eventParam)
        {
            try
            {
                // set check box that was clicked and clear the other check box
                SetCheckBox(_countCheckBox,   (component == _countPanel));
                SetCheckBox(_percentCheckBox, (component == _percentPanel));

                // save count selection status to config
                Configuration.SaveCountStatus(IsCheckBoxChecked(_countCheckBox));

                // trigger the panel to update
                _triggerUpdatePanel = true;
            }
            catch (Exception ex)
            {
                LogUtil.LogException(ex);
            }
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
        /// update the panel
        /// </summary>
        public void UpdatePanel()
        {
            try
            {
                // check if panel is triggered for update
                if (_triggerUpdatePanel)
                {
                    // get max total from age groups
                    int maxAgeCount = Math.Max(_finalCount.children.total,
                                      Math.Max(_finalCount.teens   .total,
                                      Math.Max(_finalCount.youngs  .total,
                                      Math.Max(_finalCount.adults  .total,
                                               _finalCount.seniors .total))));

                    // display results for each age group
                    DisplayDataRow(_children, _finalCount.children, false);
                    DisplayDataRow(_teens,    _finalCount.teens,    false);
                    DisplayDataRow(_youngs,   _finalCount.youngs,   false);
                    DisplayDataRow(_adults,   _finalCount.adults,   false);
                    DisplayDataRow(_seniors,  _finalCount.seniors,  false);

                    // display amount bars for age groups
                    if (maxAgeCount == 0)
                    {
                        _children.amountBar.fillAmount = 0f;
                        _teens   .amountBar.fillAmount = 0f;
                        _youngs  .amountBar.fillAmount = 0f;
                        _adults  .amountBar.fillAmount = 0f;
                        _seniors .amountBar.fillAmount = 0f;
                    }
                    else
                    {
                        _children.amountBar.fillAmount = (float)_finalCount.children.total / maxAgeCount;
                        _teens   .amountBar.fillAmount = (float)_finalCount.teens   .total / maxAgeCount;
                        _youngs  .amountBar.fillAmount = (float)_finalCount.youngs  .total / maxAgeCount;
                        _adults  .amountBar.fillAmount = (float)_finalCount.adults  .total / maxAgeCount;
                        _seniors .amountBar.fillAmount = (float)_finalCount.seniors .total / maxAgeCount;
                    }

                    // get max total from ages
                    maxAgeCount = 0;
                    for (int i = 0; i < _age.Length; i++)
                    {
                        maxAgeCount = Math.Max(_finalCount.age[i].total, maxAgeCount);
                    }

                    // display results for each age
                    for (int i = 0; i < _age.Length; i++)
                    {
                        // display data row
                        DisplayDataRow(_age[i], _finalCount.age[i], false);

                        // display amount bar for age
                        if (maxAgeCount == 0)
                        {
                            _age[i].amountBar.fillAmount = 0f;
                        }
                        else
                        {
                            _age[i].amountBar.fillAmount = (float)_finalCount.age[i].total / maxAgeCount;
                        }
                    }

                    // dipslay results for total, moving in, and deceased
                    DisplayDataRow(_total,    _finalCount.total,    false);
                    DisplayDataRow(_movingIn, _finalCount.movingIn, true);
                    DisplayDataRow(_deceased, _finalCount.deceased, true);

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
        /// <param name="dataRowUI">the data row UI on which to display the data</param>
        /// <param name="dataRow">the data to be displayed</param>
        /// <param name="useRowTotalForPercent">whether or not education levels should be displayed as percent of total for that row</param>
        private void DisplayDataRow(DataRowUI dataRowUI, DataRow dataRow, bool useRowTotalForPercent)
        {
            // check if count or percent
            if (IsCheckBoxChecked(_countCheckBox))
            {
                // display counts
                dataRowUI.eduLevel0.text = dataRow.eduLevel0.ToString("N0", LocaleManager.cultureInfo);
                dataRowUI.eduLevel1.text = dataRow.eduLevel1.ToString("N0", LocaleManager.cultureInfo);
                dataRowUI.eduLevel2.text = dataRow.eduLevel2.ToString("N0", LocaleManager.cultureInfo);
                dataRowUI.eduLevel3.text = dataRow.eduLevel3.ToString("N0", LocaleManager.cultureInfo);
                dataRowUI.total    .text = dataRow.total    .ToString("N0", LocaleManager.cultureInfo);
                dataRowUI.movingIn .text = dataRow.movingIn .ToString("N0", LocaleManager.cultureInfo);
                dataRowUI.deceased .text = dataRow.deceased .ToString("N0", LocaleManager.cultureInfo);
            }
            else
            {
                // display percents
                string format = (IsCheckBoxChecked(_ageCheckBox) ? "F3" : "F0");
                dataRowUI.eduLevel0.text = FormatPercent(dataRow.eduLevel0, (useRowTotalForPercent ? dataRow.total : _finalCount.total.total), format);
                dataRowUI.eduLevel1.text = FormatPercent(dataRow.eduLevel1, (useRowTotalForPercent ? dataRow.total : _finalCount.total.total), format);
                dataRowUI.eduLevel2.text = FormatPercent(dataRow.eduLevel2, (useRowTotalForPercent ? dataRow.total : _finalCount.total.total), format);
                dataRowUI.eduLevel3.text = FormatPercent(dataRow.eduLevel3, (useRowTotalForPercent ? dataRow.total : _finalCount.total.total), format);
                dataRowUI.total    .text = FormatPercent(dataRow.total,     (useRowTotalForPercent ? dataRow.total : _finalCount.total.total), format);
                dataRowUI.movingIn .text = FormatPercent(dataRow.movingIn,  _finalCount.movingIn.total, format);
                dataRowUI.deceased .text = FormatPercent(dataRow.deceased,  _finalCount.deceased.total, format);
            }
        }

        /// <summary>
        /// format a value as percent of a total
        /// </summary>
        /// <param name="value">the value</param>
        /// <param name="total">the total</param>
        /// <returns></returns>
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
        /// called after ResidentialBuildingAI.SimulationStepActive to count residents
        /// </summary>
        /// <param name="buildingID">building instance ID</param>
        /// <param name="data">building data</param>
        /// <param name="fromPatch">whether or not called from patch</param>
        public void ResidentialSimulationStepActive(ushort buildingID, ref Building data, bool fromPatch)
        {
            // logic is copied from ResidentialBuildingAI.SimulationStepActive
            // only the parts dealing with counting population are copied
            // ResidentialBuildingAI.SimulationStepActive calls GetHomeBehaviour
            // ResidentialBuildingAI does not have GetHomeBehaviour, so CommonBuildingAI.GetHomeBehaviour ends up being called
            // CommonBuildingAI.GetHomeBehaviour calls CitizenUnit.GetCitizenHomeBehaviour for each home citizen unit in the building
            // CitizenUnit.GetCitizenHomeBehaviour calls Citizen.GetCitizenHomeBehaviour for each of the up to 5 citizens in the unit
            // Citizen.GetCitizenHomeBehaviour counts citizens that are not deceased and not moving in even though moving in and deceased are counted here
            // the same logic used in Citizen.GetCitizenHomeBehaviour to determine age group and education level is used here
            // if the building is Completed or Upgrading, then:
            //     ResidentialBuildingAI.SimulationStepActive calls District[0].AddResidentialData passing the citizen count from the building
            //     District[0] is the base district for the entire city (as opposed to districts created by the player)
            //     District[0].AddResidentialData adds the citizen count from the building to the temp population counter for the district
            // later, District[0].SimulationStep will copy the temp counter to the final counter and reset the temp counter (see DistrictSimulationiStep below)
            // the final popultion counter is the one displayed on the PopulationInfoViewPanl and this panel

            try
            {
                // skip if this is first time thru from patch because only some of the buildings are processed
                if (fromPatch)
                {
                    if (_districtCounter == 0)
                    {
                        return;
                    }
                }

                // make sure managers exist
                if (!CitizenManager.exists)
                {
                    return;
                }

                // building must be completed or upgrading
                if ((data.m_flags & (Building.Flags.Completed | Building.Flags.Upgrading)) != 0)
                {
                    // do the citizen units
                    int unitCounter = 0;
                    CitizenManager instance = CitizenManager.instance;
                    uint maximumCitizenUnits = instance.m_units.m_size;
                    uint citizenUnit = data.m_citizenUnits;
                    while (citizenUnit != 0)
                    {
                        // not sure if Flags will ever be other than Home for ResidentialBuildingAI, but check anyway
                        if ((instance.m_units.m_buffer[citizenUnit].m_flags & CitizenUnit.Flags.Home) != 0)
                        {
                            // do each of the up to 5 citizens
                            for (int i = 0; i < 5; i++)
                            {
                                // citizen ID must be defined
                                uint citizenID = instance.m_units.m_buffer[citizenUnit].GetCitizen(i);
                                if (citizenID != 0)
                                {
                                    // increment the citizen temp count
                                    IncrementCitizenTempCount(instance.m_citizens.m_buffer[citizenID]);
                                }
                            }
                        }

                        // get the next citizen unit
                        citizenUnit = instance.m_units.m_buffer[citizenUnit].m_nextUnit;

                        // check for error (e.g. circular reference)
                        if (++unitCounter > maximumCitizenUnits)
                        {
                            LogUtil.LogError("Invalid list detected!" + Environment.NewLine + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogException(ex);
            }
        }

        /// <summary>
        /// called after District.SimulationStep to copy temp count to final count
        /// </summary>
        /// <param name="districtID">district instance ID</param>
        /// <param name="fromPatch">whether or not called from patch</param>
        public void DistrictSimulationStep(byte districtID, bool fromPatch)
        {
            try
            {
                // only do the city-wide district
                if (districtID == 0)
                {
                    // skip the first time called from patch because temp will have only a partial count
                    if (fromPatch)
                    {
                        if (_districtCounter++ == 0)
                        {
                            return;
                        }
                    }

                    // logic copied from District.SimulateStep
                    if (_tempCount.total.total < 10000000)
                    {
                        // copy temp count to final count
                        _finalCount.Copy(_tempCount);

                        // trigger panel update
                        _triggerUpdatePanel = true;
                    }

                    // reset temp count
                    _tempCount.Reset();
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogException(ex);
            }
        }

        /// <summary>
        /// increment the appropriate citizen temp count based on the citizen's demographics
        /// </summary>
        /// <param name="citizen">citizen instance</param>
        private void IncrementCitizenTempCount(Citizen citizen)
        {
            // get the age group, real age, and education level
            Citizen.AgeGroup ageGroup = Citizen.GetAgeGroup(citizen.Age);
            int realAge = Mathf.Clamp((int)(citizen.Age * RealAgePerGameAge), 0, MaxRealAge);
            Citizen.Education educationLevel = citizen.EducationLevel;

            // check if deceased
            if (citizen.Dead)
            {
                // increment deceased count for age group
                switch (ageGroup)
                {
                    case Citizen.AgeGroup.Child:  _tempCount.children.deceased++; break;
                    case Citizen.AgeGroup.Teen:   _tempCount.teens   .deceased++; break;
                    case Citizen.AgeGroup.Young:  _tempCount.youngs  .deceased++; break;
                    case Citizen.AgeGroup.Adult:  _tempCount.adults  .deceased++; break;
                    case Citizen.AgeGroup.Senior: _tempCount.seniors .deceased++; break;
                }

                // increment deceased count for age
                _tempCount.age[realAge].deceased++;

                // increment deceased count for education level
                switch (educationLevel)
                {
                    case Citizen.Education.Uneducated:   _tempCount.deceased.eduLevel0++; break;
                    case Citizen.Education.OneSchool:    _tempCount.deceased.eduLevel1++; break;
                    case Citizen.Education.TwoSchools:   _tempCount.deceased.eduLevel2++; break;
                    case Citizen.Education.ThreeSchools: _tempCount.deceased.eduLevel3++; break;
                }
            }
            else
            {
                // check if moving in
                if ((citizen.m_flags & Citizen.Flags.MovingIn) != 0)
                {
                    // increment moving in count for age group
                    switch (ageGroup)
                    {
                        case Citizen.AgeGroup.Child:  _tempCount.children.movingIn++; break;
                        case Citizen.AgeGroup.Teen:   _tempCount.teens   .movingIn++; break;
                        case Citizen.AgeGroup.Young:  _tempCount.youngs  .movingIn++; break;
                        case Citizen.AgeGroup.Adult:  _tempCount.adults  .movingIn++; break;
                        case Citizen.AgeGroup.Senior: _tempCount.seniors .movingIn++; break;
                    }

                    // increment moving in count for age
                    _tempCount.age[realAge].movingIn++;

                    // increment moving in count for education level
                    switch (educationLevel)
                    {
                        case Citizen.Education.Uneducated:   _tempCount.movingIn.eduLevel0++; break;
                        case Citizen.Education.OneSchool:    _tempCount.movingIn.eduLevel1++; break;
                        case Citizen.Education.TwoSchools:   _tempCount.movingIn.eduLevel2++; break;
                        case Citizen.Education.ThreeSchools: _tempCount.movingIn.eduLevel3++; break;
                    }
                }
                else
                {
                    // normal citizen (i.e. not deceased and not moving in)

                    // get the data row for the age group
                    DataRow drAgeGroup = null;
                    switch (ageGroup)
                    {
                        case Citizen.AgeGroup.Child:  drAgeGroup = _tempCount.children; break;
                        case Citizen.AgeGroup.Teen:   drAgeGroup = _tempCount.teens;    break;
                        case Citizen.AgeGroup.Young:  drAgeGroup = _tempCount.youngs;   break;
                        case Citizen.AgeGroup.Adult:  drAgeGroup = _tempCount.adults;   break;
                        case Citizen.AgeGroup.Senior: drAgeGroup = _tempCount.seniors;  break;
                    }

                    // get the data row for the age
                    DataRow drAge = _tempCount.age[realAge];

                    // increment count for age group and age based on education level
                    switch (educationLevel)
                    {
                        case Citizen.Education.Uneducated:   drAgeGroup.eduLevel0++; drAge.eduLevel0++; break;
                        case Citizen.Education.OneSchool:    drAgeGroup.eduLevel1++; drAge.eduLevel1++; break;
                        case Citizen.Education.TwoSchools:   drAgeGroup.eduLevel2++; drAge.eduLevel2++; break;
                        case Citizen.Education.ThreeSchools: drAgeGroup.eduLevel3++; drAge.eduLevel3++; break;
                    }
                }
            }
        }
    }
}
