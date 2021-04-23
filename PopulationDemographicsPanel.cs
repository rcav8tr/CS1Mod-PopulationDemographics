using ColossalFramework.UI;
using ColossalFramework;
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
        // text
        UIFont _textFont = null;
        private readonly Color32 DataTextColor = new Color32(185, 221, 254, 255);
        private readonly Color32 HeadingTextColor = new Color32(206, 248, 0, 255);

        // common count attributes
        const float CountWidth = 67f;
        const float TextHeight = 15f;
        const float LabelSpacing = 4f;

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

        // the data rows by age group
        private class DataRows
        {
            public DataRow children = new DataRow();
            public DataRow teens    = new DataRow();
            public DataRow youngs   = new DataRow();
            public DataRow adults   = new DataRow();
            public DataRow seniors  = new DataRow();
            public DataRow total    = new DataRow();
            public DataRow movingIn = new DataRow();
            public DataRow deceased = new DataRow();

            public void Reset()
            {
                // reset each data row
                children.Reset();
                teens.Reset();
                youngs.Reset();
                adults.Reset();
                seniors.Reset();
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
            public UILabel eduLevel0;
            public UILabel eduLevel1;
            public UILabel eduLevel2;
            public UILabel eduLevel3;
            public UILabel total;
            public UILabel movingIn;
            public UILabel deceased;
        }

        // UI elements for each data row
        private DataRowUI _heading;
        private DataRowUI _children;
        private DataRowUI _teens;
        private DataRowUI _youngs;
        private DataRowUI _adults;
        private DataRowUI _seniors;
        private DataRowUI _total;
        private DataRowUI _movingIn;
        private DataRowUI _deceased;

        // UI elements for count/percent buttons
        private UIPanel _countPanel;
        private UIPanel _percentPanel;
        private UISprite _countCheckBox;
        private UISprite _percentCheckBox;

        // other UI elements
        private UIButton _closeButton;

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
                isVisible = true;

                // get the PopulationInfoViewPanel panel (displayed when the user clicks on the Population info view button)
                PopulationInfoViewPanel populationPanel = UIView.library.Get<PopulationInfoViewPanel>(typeof(PopulationInfoViewPanel).Name);
                if (populationPanel == null)
                {
                    Debug.LogError("Unable to find PopulationInfoViewPanel.");
                    return;
                }

                // place panel to the right of PopulationInfoViewPanel
                relativePosition = new Vector3(populationPanel.component.size.x - 1f, 0f);

                // copy text font from the Population label
                UILabel populationLabel = populationPanel.Find<UILabel>("Population");
                if (populationLabel == null)
                {
                    Debug.LogError("Unable to find Population label on PopulationInfoViewPanel.");
                    return;
                }
                _textFont = populationLabel.font;

                // create heading row
                float top = 45f;
                if (!CreateDataRow(top, "Heading", "", out _heading)) return;

                // adjust heading properties
                _heading.eduLevel0.text = "Unedu";
                _heading.eduLevel1.text = "Educated";
                _heading.eduLevel2.text = "Well Edu";
                _heading.eduLevel3.text = "High Edu";
                _heading.total.text     = "Total";
                _heading.deceased.text  = "Deceased";
                _heading.movingIn.text  = "MovingIn";

                _heading.eduLevel0.tooltip = "Uneducated - Elementary School not completed";
                _heading.eduLevel1.tooltip = "Educated - completed Elementary School";
                _heading.eduLevel2.tooltip = "Well Educated - completed High School";
                _heading.eduLevel3.tooltip = "Highly Educated - completed University";

                _heading.eduLevel0.textScale =
                    _heading.eduLevel1.textScale =
                    _heading.eduLevel2.textScale =
                    _heading.eduLevel3.textScale =
                    _heading.total.textScale =
                    _heading.deceased.textScale =
                    _heading.movingIn.textScale = 0.75f;

                _heading.eduLevel0.textColor =
                    _heading.eduLevel1.textColor =
                    _heading.eduLevel2.textColor =
                    _heading.eduLevel3.textColor =
                    _heading.total.textColor =
                    _heading.deceased.textColor =
                    _heading.movingIn.textColor = HeadingTextColor;

                // create lines after headings
                top += 15f;
                CreateLines(top, "Heading");

                // create data rows
                top += 4f;
                if (!CreateDataRow(top, "Children", "Children",     out _children)) return; top += 15f;
                if (!CreateDataRow(top, "Teens",    "Teens",        out _teens   )) return; top += 15f;
                if (!CreateDataRow(top, "Young",    "Young Adults", out _youngs  )) return; top += 15f;
                if (!CreateDataRow(top, "Adults",   "Adults",       out _adults  )) return; top += 15f;
                if (!CreateDataRow(top, "Seniors",  "Seniors",      out _seniors )) return; top += 15f;

                // create total data row
                CreateLines(top, "Totals");
                top += 4f;
                if (!CreateDataRow(top, "Total", "Total", out _total)) return; top += 15f;

                // create other data rows
                top += 12f;
                if (!CreateDataRow(top, "MovingIn", "Moving In", out _movingIn)) return; top += 15f;
                if (!CreateDataRow(top, "Deceased", "Deceased",  out _deceased)) return; top += 15f;

                // hide duplicates for moving in and deceased
                _movingIn.movingIn.isVisible = false;
                _movingIn.deceased.isVisible = false;
                _deceased.movingIn.isVisible = false;
                _deceased.deceased.isVisible = false;

                // set panel width and height according to the labels
                width = _children.deceased.relativePosition.x + _children.deceased.size.x + _children.description.relativePosition.x;
                height = _deceased.deceased.relativePosition.y + _deceased.deceased.size.y + 5f;

                // create the title label
                UILabel title = AddUIComponent<UILabel>();
                if (title == null)
                {
                    Debug.LogError($"Unable to create title label on panel [{name}].");
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
                    Debug.LogError($"Unable to create population icon on panel [{name}].");
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
                    Debug.LogError($"Unable to create close button on panel [{name}].");
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

                // create count/percent panels
                CreateCountPercentPanel(_movingIn.description.relativePosition.y, "CountOption",   "Count",   out _countPanel,   out _countCheckBox);
                CreateCountPercentPanel(_deceased.description.relativePosition.y, "PercentOption", "Percent", out _percentPanel, out _percentCheckBox);
                SetCheckBox(_countCheckBox, true);

                // make sure manager exists
                if (!Singleton<BuildingManager>.exists)
                {
                    Debug.LogError($"BuildingManager not ready during panel initialization.");
                    return;
                }

                // initialize population counts, do each building
                Building[] buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
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
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// handle Close button clicked
        /// </summary>
        private void CloseButton_eventClicked(UIComponent component, UIMouseEventParameter eventParam)
        {
            // hide this panel
            isVisible = false;
        }

        /// <summary>
        /// create a UI data row
        /// </summary>
        /// <param name="top">top position</param>
        /// <param name="namePrefix">component name prefix</param>
        /// <param name="text">description text</param>
        /// <param name="dataRow">ouitput the UI data row</param>
        /// <returns>success status</returns>
        private bool CreateDataRow(float top, string namePrefix, string text, out DataRowUI dataRow)
        {
            // create new worker data
            dataRow = new DataRowUI();

            // create label for description
            dataRow.description = AddUIComponent<UILabel>();
            if (dataRow.description == null)
            {
                Debug.LogError($"Unable to create description label for [{namePrefix}] on panel [{name}].");
                return false;
            }
            dataRow.description.name = namePrefix + "Description";
            dataRow.description.font = _textFont;
            dataRow.description.text = text;
            dataRow.description.textAlignment = UIHorizontalAlignment.Left;
            dataRow.description.verticalAlignment = UIVerticalAlignment.Bottom;
            dataRow.description.textScale = 0.875f;
            dataRow.description.textColor = HeadingTextColor;
            dataRow.description.autoSize = false;
            dataRow.description.size = new Vector2(100f, TextHeight);
            dataRow.description.relativePosition = new Vector3(8f, top);
            dataRow.description.isVisible = true;

            // create count labels
            if (!CreateCountLabel(0f,  top, namePrefix + "Level0",   dataRow.description, out dataRow.eduLevel0)) return false;
            if (!CreateCountLabel(0f,  top, namePrefix + "Level1",   dataRow.eduLevel0,   out dataRow.eduLevel1)) return false;
            if (!CreateCountLabel(0f,  top, namePrefix + "Level2",   dataRow.eduLevel1,   out dataRow.eduLevel2)) return false;
            if (!CreateCountLabel(0f,  top, namePrefix + "Level3",   dataRow.eduLevel2,   out dataRow.eduLevel3)) return false;
            if (!CreateCountLabel(0f,  top, namePrefix + "Total",    dataRow.eduLevel3,   out dataRow.total    )) return false;
            
            if (!CreateCountLabel(12f, top, namePrefix + "MovingIn", dataRow.total,       out dataRow.movingIn )) return false;
            if (!CreateCountLabel(0f,  top, namePrefix + "Deceased", dataRow.movingIn,    out dataRow.deceased )) return false;

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
        private bool CreateCountLabel(float leftAdd, float top, string labelName, UILabel previousLabel, out UILabel count)
        {
            count = AddUIComponent<UILabel>();
            if (count == null)
            {
                Debug.LogError($"Unable to create label [{labelName}] on panel [{name}].");
                return false;
            }
            count.name = labelName;
            count.font = _textFont;
            count.text = "000,000";
            count.textAlignment = UIHorizontalAlignment.Right;
            count.verticalAlignment = UIVerticalAlignment.Bottom;
            count.textScale = 0.875f;
            count.textColor = DataTextColor;
            count.autoSize = false;
            count.size = new Vector2(CountWidth, TextHeight);
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
        private bool CreateLines(float top, string namePrefix)
        {
            // compute line color
            const float ColorMult = 0.8f;
            Color32 lineColor = new Color32((byte)(HeadingTextColor.r * ColorMult), (byte)(HeadingTextColor.g * ColorMult), (byte)(HeadingTextColor.b * ColorMult), 255);

            // a line is needed for each column except description
            for (int i = 0; i < 7; ++i)
            {
                UISprite line = AddUIComponent<UISprite>();
                if (line == null)
                {
                    Debug.LogError($"Unable to create [{namePrefix}] line sprite [{i}] on panel [{name}].");
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
        /// create a panel to hold a count vs percent check box and label
        /// </summary>
        /// <param name="top">top position of the panel</param>
        /// <param name="namePrefix">component name prefix</param>
        /// <param name="labelText">text for the label</param>
        /// <param name="panel">output the panel</param>
        /// <param name="checkBox">output the check box</param>
        /// <returns></returns>
        private bool CreateCountPercentPanel(float top, string namePrefix, string labelText, out UIPanel panel, out UISprite checkBox)
        {
            // satisfy compiler
            panel = null;
            checkBox = null;

            // create a new panel
            panel = AddUIComponent<UIPanel>();
            if (panel == null)
            {
                Debug.LogError($"Unable to create panel [{namePrefix}] on panel [{name}].");
                return false;
            }
            panel.name = namePrefix + "Panel";
            panel.size = new Vector2(90f, TextHeight);
            panel.relativePosition = new Vector3(width - panel.size.x - 10f, top);
            panel.isVisible = true;

            // set up click event handler
            // a click on any contained component triggers a click event on the panel
            // therefore, each individual component does not need its own click event handler
            panel.eventClicked += DisplayOption_eventClicked;

            // create the checkbox (i.e. a sprite)
            checkBox = panel.AddUIComponent<UISprite>();
            if (checkBox == null)
            {
                Debug.LogError($"Unable to create check box sprite on panel [{panel.name}].");
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
                Debug.LogError($"Unable to create label on panel [{panel.name}].");
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
        /// Click event handler for display options
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

                // trigger the panel to update
                _triggerUpdatePanel = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
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
                    // display results for each data row
                    DisplayDataRow(_children, _finalCount.children, false);
                    DisplayDataRow(_teens,    _finalCount.teens,    false);
                    DisplayDataRow(_youngs,   _finalCount.youngs,   false);
                    DisplayDataRow(_adults,   _finalCount.adults,   false);
                    DisplayDataRow(_seniors,  _finalCount.seniors,  false);
                    DisplayDataRow(_total,    _finalCount.total,    false);
                    DisplayDataRow(_movingIn, _finalCount.movingIn, true);
                    DisplayDataRow(_deceased, _finalCount.deceased, true);

                    // wait for next trigger
                    _triggerUpdatePanel = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
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
                dataRowUI.eduLevel0.text = dataRow.eduLevel0.ToString("N0", LocaleManager.cultureInfo);
                dataRowUI.eduLevel1.text = dataRow.eduLevel1.ToString("N0", LocaleManager.cultureInfo);
                dataRowUI.eduLevel2.text = dataRow.eduLevel2.ToString("N0", LocaleManager.cultureInfo);
                dataRowUI.eduLevel3.text = dataRow.eduLevel3.ToString("N0", LocaleManager.cultureInfo);
                dataRowUI.total.text     = dataRow.total.ToString("N0",     LocaleManager.cultureInfo);
                dataRowUI.movingIn.text  = dataRow.movingIn.ToString("N0",  LocaleManager.cultureInfo);
                dataRowUI.deceased.text  = dataRow.deceased.ToString("N0",  LocaleManager.cultureInfo);
            }
            else
            {
                dataRowUI.eduLevel0.text = FormatPercent(dataRow.eduLevel0, (useRowTotalForPercent ? dataRow.total : _finalCount.total.total));
                dataRowUI.eduLevel1.text = FormatPercent(dataRow.eduLevel1, (useRowTotalForPercent ? dataRow.total : _finalCount.total.total));
                dataRowUI.eduLevel2.text = FormatPercent(dataRow.eduLevel2, (useRowTotalForPercent ? dataRow.total : _finalCount.total.total));
                dataRowUI.eduLevel3.text = FormatPercent(dataRow.eduLevel3, (useRowTotalForPercent ? dataRow.total : _finalCount.total.total));
                dataRowUI.total.text     = FormatPercent(dataRow.total,     (useRowTotalForPercent ? dataRow.total : _finalCount.total.total));
                dataRowUI.movingIn.text  = FormatPercent(dataRow.movingIn,  _finalCount.movingIn.total);
                dataRowUI.deceased.text  = FormatPercent(dataRow.deceased,  _finalCount.deceased.total);
            }
        }

        /// <summary>
        /// format a value as percent of a total
        /// </summary>
        /// <param name="value">the value</param>
        /// <param name="total">the total</param>
        /// <returns></returns>
        private string FormatPercent(int value, int total)
        {
            float percent = 0f;
            if (total != 0)
            {
                percent = 100f * value / total;
            }
            return percent.ToString("F0", LocaleManager.cultureInfo) + "%";
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
                if (!Singleton<CitizenManager>.exists)
                {
                    return;
                }

                // building must be completed or upgrading
                if ((data.m_flags & (Building.Flags.Completed | Building.Flags.Upgrading)) != 0)
                {
                    // do the citizen units
                    int unitCounter = 0;
                    CitizenManager instance = Singleton<CitizenManager>.instance;
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
                        if (++unitCounter > CitizenManager.MAX_UNIT_COUNT)
                        {
                            Debug.LogError("Invalid list detected!" + Environment.NewLine + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
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
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// increment the appropriate citizen temp count based on the citizen's demographics
        /// </summary>
        /// <param name="citizen">citizen instance</param>
        private void IncrementCitizenTempCount(Citizen citizen)
        {
            // get the age group and education level
            Citizen.AgeGroup ageGroup = Citizen.GetAgeGroup(citizen.Age);
            Citizen.Education educationLevel = citizen.EducationLevel;

            // check if deceased
            if (citizen.Dead)
            {
                // increment deceased count for age group
                switch (ageGroup)
                {
                    case Citizen.AgeGroup.Child:  _tempCount.children.deceased++; break;
                    case Citizen.AgeGroup.Teen:   _tempCount.teens.deceased++;    break;
                    case Citizen.AgeGroup.Young:  _tempCount.youngs.deceased++;   break;
                    case Citizen.AgeGroup.Adult:  _tempCount.adults.deceased++;   break;
                    case Citizen.AgeGroup.Senior: _tempCount.seniors.deceased++;  break;
                }

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
                        case Citizen.AgeGroup.Teen:   _tempCount.teens.movingIn++;    break;
                        case Citizen.AgeGroup.Young:  _tempCount.youngs.movingIn++;   break;
                        case Citizen.AgeGroup.Adult:  _tempCount.adults.movingIn++;   break;
                        case Citizen.AgeGroup.Senior: _tempCount.seniors.movingIn++;  break;
                    }

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

                    // increment count for age group and education level
                    switch (educationLevel)
                    {
                        case Citizen.Education.Uneducated:   drAgeGroup.eduLevel0++; break;
                        case Citizen.Education.OneSchool:    drAgeGroup.eduLevel1++; break;
                        case Citizen.Education.TwoSchools:   drAgeGroup.eduLevel2++; break;
                        case Citizen.Education.ThreeSchools: drAgeGroup.eduLevel3++; break;
                    }
                }
            }
        }

        /// <summary>
        /// called when panel is destroyed
        /// </summary>
        public override void OnDestroy()
        {
            // do base processing
            base.OnDestroy();

            // remove event handlers
            if (_countPanel != null)
            {
                _countPanel.eventClicked -= DisplayOption_eventClicked;
            }
            if (_percentPanel != null)
            {
                _percentPanel.eventClicked -= DisplayOption_eventClicked;
            }
            if (_closeButton != null)
            {
                _closeButton.eventClicked -= CloseButton_eventClicked;
            }
        }
    }
}
