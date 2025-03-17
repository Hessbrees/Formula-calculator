using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "FormulaSettingsSO", menuName = "SoE/Formula/FormulaSettings", order = 0)]
public class FormulaSettingsSO : ScriptableObject
    {
        [Title("Name || Type")]
        public List<FormulaSettingData> formulaSettingDataList;

        [Button]
        [PropertySpace(20)]
        public void UpdateFormulaSetting()
        {
            var dataTypesNum = Enum.GetValues(typeof(DataType)).Cast<DataType>().ToList();
            var statSourcesNum = Enum.GetValues(typeof(StatSource)).Cast<StatSource>().ToList();
            var statTypesNum = Enum.GetValues(typeof(StatType)).Cast<StatType>().ToList();
            var parameterNum = Enum.GetValues(typeof(ParameterName)).Cast<ParameterName>().ToList();
            var resourceNum = Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>().ToList();


            foreach (var type in dataTypesNum)
            {
                switch (type)
                {
                    case DataType.currentHealth or DataType.currentEnergy or DataType.currentLevel:
                        CreateSettingsData(type, statSourcesNum);
                        break;
                    case DataType.statType:
                        CreateSettingsData(type, statSourcesNum, statTypesNum);
                        break;
                    case DataType.itemLevelBase or DataType.itemLevelTotal or DataType.playerItemLevelTotal:
                        CreateSettingsData(type);
                        break;
                    case DataType.eventFilter:
                        CreateSettingsData(type, parameterNum);
                        break;
                    case DataType.currentResource or DataType.totalResource:
                        CreateSettingsData(type, resourceNum);
                        break;
                }
            }
        }
        private void CreateSettingsData(DataType type, List<StatSource> statSourcesNum, List<StatType> statTypesNum)
        {
            foreach (var statSource in statSourcesNum)
            {
                foreach (var statType in statTypesNum)
                {
                    FormulaSettingData formulaSettingData = new();
                    formulaSettingData.dataType = type;
                    formulaSettingData.statType = statType;
                    formulaSettingData.statSource = statSource;
                    TryAddFormulaSettingToList(formulaSettingData);
                }
            }
        }
        private void CreateSettingsData(DataType type, List<ParameterName> parameterNum)
        {
            foreach (var parameterName in parameterNum)
            {
                FormulaSettingData formulaSettingData = new();
                formulaSettingData.dataType = type;
                formulaSettingData.parameterName = parameterName;
                TryAddFormulaSettingToList(formulaSettingData);
            }
        }
        private void CreateSettingsData(DataType type, List<StatSource> statSourcesNum)
        {
            foreach (var statSource in statSourcesNum)
            {
                FormulaSettingData formulaSettingData = new();
                formulaSettingData.dataType = type;
                formulaSettingData.statSource = statSource;
                TryAddFormulaSettingToList(formulaSettingData);
            }
        }
        private void CreateSettingsData(DataType type, List<ResourceType> resourceNum)
        {
            foreach (var resourceType in resourceNum)
            {
                FormulaSettingData formulaSettingData = new();
                formulaSettingData.dataType = type;
                formulaSettingData.resourceType = resourceType;
                TryAddFormulaSettingToList(formulaSettingData);
            }
        }
        private void CreateSettingsData(DataType type)
        {
            FormulaSettingData formulaSettingData = new();
            formulaSettingData.dataType = type;
            TryAddFormulaSettingToList(formulaSettingData);
        }
        
        private void TryAddFormulaSettingToList(FormulaSettingData data)
        {
            foreach (var item in formulaSettingDataList)
            {
                if (item.dataType == data.dataType)
                {
                    if (data.statSource == StatSource.none) return;

                    switch (item.dataType)
                    {
                        case DataType.currentHealth or DataType.currentEnergy or DataType.currentLevel:
                            if (item.statSource == data.statSource) return;
                            break;
                        case DataType.statType:
                            if (item.statType == data.statType && item.statSource == data.statSource) return;
                            break;
                        case DataType.itemLevelTotal or DataType.itemLevelBase or DataType.playerItemLevelTotal:
                            return;
                        case DataType.eventFilter:
                            if (item.parameterName == data.parameterName) return;
                            break;
                        case DataType.currentResource:
                            if (item.resourceType == data.resourceType) return;
                            break;
                        case DataType.totalResource:
                            if (item.resourceType == data.resourceType) return;
                            break;
                    }
                }
            }

#if UNITY_EDITOR
            data.SetDefaultName();
#endif
            formulaSettingDataList.Add(data);
        }
        public FormulaSettingData TryFindFormula(string name)
        {
            foreach (var data in formulaSettingDataList)
            {
                if (data.itemName == name) return data;
            }

            return null;
        }
    }
    [Serializable]
    public class FormulaSettingData
    {

        [HideLabel, HorizontalGroup] public string itemName;

        [OnValueChanged("SetDefaultName")][HideLabel, HorizontalGroup] public DataType dataType;
        [OnValueChanged("SetDefaultName")][ShowIf("@dataType == DataType.statType")] public StatType statType;
        [OnValueChanged("SetDefaultName")][ShowIf("@dataType == DataType.eventFilter")] public ParameterName parameterName;
        [OnValueChanged("SetDefaultName")]
        [ShowIf("@dataType == DataType.statType || " +
            "dataType == DataType.currentHealth || " +
            "dataType == DataType.currentEnergy")]
        public StatSource statSource;
        [ShowIf("@dataType == DataType.currentResource || dataType == DataType.totalResource")]
        public ResourceType resourceType;
#if UNITY_EDITOR

        public void SetDefaultName()
        {
            string result = "";

            if (dataType == DataType.statType) result = $"{statSource}.{statType}";
            else if (dataType == DataType.currentHealth) result = $"{statSource}.health";
            else if (dataType == DataType.currentEnergy) result = $"{statSource}.energy";
            else if (dataType == DataType.currentLevel) result = $"{statSource}.currentLevel";
            else if (dataType == DataType.itemLevelBase) result = "ItemLevelBase";
            else if (dataType == DataType.itemLevelTotal) result = "ItemLevelTotal";
            else if (dataType == DataType.playerItemLevelTotal) result = "PlayerItemLevelTotal";
            else if (dataType == DataType.eventFilter) result = $"{parameterName}";
            else if (dataType == DataType.currentResource) result = $"currentResource.{resourceType}";
            else if (dataType == DataType.totalResource) result = $"totalResource.{resourceType}";
            itemName = result;
        }
#endif
    }
