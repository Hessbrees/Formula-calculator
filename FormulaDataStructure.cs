using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Sirenix.OdinInspector;

public class FormulaDataStructure
    {
        [InfoBox("$infoBox", VisibleIf = "isInfoBoxNotEmpty")]
        [OnValueChanged("ClearInfobox"), Title("Formula Equation:"), HideLabel, MultiLineProperty, Configurable]
        public string result;

        private string visibleResult;
        
        [JsonIgnore]
        public string VisibleResult => visibleResult;
        private string calculationResult;
        NumberStyles numberStyle = NumberStyles.Float;
        private ISource source;
        private IAttackable target;
        private FormulaSettingsSO formulaSettings;

        private Queue<string> valuesNotFinded = new();
        private int wrongOperatorsNumber;
        private int leftBracketsCount;
        private int rightBracketsCount;

        bool isEquationVerified;
        bool isInfoBoxNotEmpty;
        bool isFormulaWrong;
        string infoBox;

        public float CalculateMultipler(ISource source, IAttackable target)
        {
            this.source = source;
            this.target = target;

            return CalculateMultipler();
        }
  
        public float CalculateMultipler()
        {
            statSources.Clear();
            isFormulaWrong = false;

            formulaSettings = Resources.Load<FormulaSettingsSO>("FormulaSettingsData/FormulaSettingsSO");

            if (formulaSettings == null)
            {
                LogTool.Log(LogDataType.Formula, "FormulaSettings is null");
                return 0;
            }

            calculationResult = result;
            calculationResult = calculationResult.Replace(",", ".");

            ConvertMultiplicationBracetsToOperators();

            float finalValue = 0;

            while (calculationResult.Length > 0)
            {
                string currentEquation;
                finalValue = CalculateDeepestBracket(FindDeepestBracket(), out currentEquation);

                LogTool.Log(LogDataType.Formula, $"Calculated value: {finalValue}");

                UpdateEquation(currentEquation, finalValue);
            }

            LogTool.Log(LogDataType.Formula, $"Calculated finalValue: {finalValue}");

            return finalValue;
        }

        private Color32 GetButtonColor()
        {
#if UNITY_EDITOR
            Sirenix.Utilities.Editor.GUIHelper.RequestRepaint();
#endif

            if (isEquationVerified) return new Color32(0, 255, 0, 255);
            else if (isInfoBoxNotEmpty) return new Color32(255, 0, 0, 255);
            else return new Color32(255, 255, 255, 255);
        }

        public string CheckButton(ISource source, IAttackable target)
        {
            if (visibleResult == result) return infoBox;

            visibleResult = result;
            infoBox = "";
            wrongOperatorsNumber = 0;
            leftBracketsCount = 0;
            rightBracketsCount = 0;
            valuesNotFinded.Clear();

            CalculateMultipler(source, target);

            while (valuesNotFinded.Count > 0)
            {
                infoBox += $"Value not exist in data: {valuesNotFinded.Dequeue()} \n";
            }
            if (wrongOperatorsNumber != 0)
            {
                infoBox += $"Wrong number of operators: {wrongOperatorsNumber} \n";
            }
            if (leftBracketsCount != rightBracketsCount)
            {
                infoBox += $"Wrong number of brackets: left {leftBracketsCount} right {rightBracketsCount} \n";
            }

            LogTool.Log(LogDataType.Formula, infoBox);

            if (infoBox == "")
            {
                isEquationVerified = true;
            }
            else
            {
                isInfoBoxNotEmpty = true;
            }

            return infoBox;
        }
        private void ClearInfobox()
        {
            if (visibleResult == result) return;

            isEquationVerified = false;
            isInfoBoxNotEmpty = false;
            infoBox = "";

        }
        private void UpdateEquation(string item, float value)
        {
            if (calculationResult.Contains("(" + item + ")"))
            {
                calculationResult = calculationResult.Replace("(" + item + ")", value.ToString());
            }
            else if (item == "")
            {
                calculationResult = "";
            }
            else
            {
                calculationResult.Replace(item, value.ToString());
            }
        }
        private int FindDeepestBracket()
        {
            int leftBracketCount = 0;
            int maxCount = 0;

            for (int i = 0; i < calculationResult.Length; i++)
            {
                if (calculationResult[i] == '(')
                {
                    this.leftBracketsCount++;

                    leftBracketCount++;
                    if (leftBracketCount >= maxCount) maxCount = leftBracketCount;
                }
                else if (calculationResult[i] == ')')
                {
                    this.rightBracketsCount++;

                    leftBracketCount--;
                }
            }

            if (leftBracketCount != 0) return 0;

            return maxCount;
        }

        private float CalculateDeepestBracket(int maxCount, out string equation)
        {
            int leftBracketCount = 0;
            int currentCount = 0;
            bool isTimeToFindEquation = false;

            equation = "";

            if (maxCount == 0)
            {
                return ParseStringEquationToValues(calculationResult);
            }

            for (int i = 0; i < calculationResult.Length; i++)
            {
                if (calculationResult[i] == '(')
                {
                    leftBracketCount++;

                    if (leftBracketCount >= currentCount) currentCount = leftBracketCount;

                    if (maxCount == currentCount)
                    {
                        isTimeToFindEquation = true;
                        continue;
                    }
                }
                else if (calculationResult[i] == ')')
                {
                    leftBracketCount--;

                    if (isTimeToFindEquation)
                    {
                        break;
                    }
                }

                if (isTimeToFindEquation)
                {
                    equation += calculationResult[i];
                }
            }

            return ParseStringEquationToValues(equation);
        }
        private void ConvertMultiplicationBracetsToOperators()
        {
            calculationResult = calculationResult.Replace(")(", ")*(");
            calculationResult = calculationResult.Replace(")[", ")*[");
            calculationResult = calculationResult.Replace("](", "]*(");
            calculationResult = calculationResult.Replace("][", "]*[");
        }
        private float ParseStringEquationToValues(string equation)
        {
            equation = equation.Replace("--", "+");

            string[] stringVariableArray = equation.Split('+', '-', '*', '/', '^');

            LogTool.Log(LogDataType.Formula, "Current equation parse " + equation);

            Stack<OperationType> operationsStack = new();

            if (equation.First() == '-')
            {
                equation = equation.Remove(0, 1);
                operationsStack.Push(OperationType.Subtraction);
            }
            else operationsStack.Push(OperationType.None);

            foreach (var item in equation)
            {
                if (item == '+' || item == '-' || item == '*' || item == '/' || item == '^')
                {
                    LogTool.Log(LogDataType.Formula, "Add operator to stack " + item.ToString());
                    OperationType operationType = ConvertStringToOperation(item);
                    operationsStack.Push(operationType);
                }
            }
            Stack<float> calculationStack = new();

            for (int i = 0; i < stringVariableArray.Length; i++)
            {
                stringVariableArray[i] = RemoveWhitespace(stringVariableArray[i]);

                if (stringVariableArray[i] == "") continue;

                calculationStack.Push(TryParseStringToValue(stringVariableArray[i]));
            }
            LogTool.Log(LogDataType.Formula, "Stack operation number " + operationsStack.Count);
            LogTool.Log(LogDataType.Formula, "Calculation operation number " + calculationStack.Count);

            if (valuesNotFinded.Count > 0) return 0;

            return CalculateFinalValue(calculationStack, operationsStack);
        }
        public string RemoveWhitespace(string input)
        {
            if (input == null) return null;

            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }
        private float TryParseStringToValue(string nameValue)
        {
            LogTool.Log(LogDataType.Formula, "Parse name to value " + nameValue);

            FormulaSettingData formulaSettingData = formulaSettings.TryFindFormula(nameValue); 

            if (formulaSettingData == null)
            {
                float value;

                nameValue = nameValue.Replace(",", ".");

                if (float.TryParse(nameValue, numberStyle, CultureInfo.InvariantCulture, out value))
                {
                    LogTool.Log(LogDataType.Formula, "Parse to basic value " + value);
                    return value;
                }
                else
                {
                    LogTool.Log(LogDataType.Formula, "Value not found " + nameValue);
                    valuesNotFinded.Enqueue(nameValue);
                    return 0;
                }
            }

            return ParseFormulaToValue(formulaSettingData);
        }

        private float ParseFormulaToValue(FormulaSettingData formulaSettingData)
        {
            AddStatSourceToCheckList(formulaSettingData.statSource);

            switch (formulaSettingData.dataType)
            {
                // Get float value from method for dataType converted from text
            }

            LogTool.Log(LogDataType.ValidateValue, "$Formula parse default");
            return 0;
        }
  
        private Queue<T> ConvertToQueue<T>(Stack<T> stackItem)
        {
            Queue<T> queue = new Queue<T>();

            while (stackItem.Count > 0)
            {
                queue.Enqueue(stackItem.Pop());
            }
            return queue;
        }

        private float CalculateFinalValue(Stack<float> calculationStack, Stack<OperationType> operationsStack)
        {
            float result = 0;
            bool isEndToMultiplication = false;
            bool isTimeToSumQueue = false;

            Queue<float> calculationQueue = ConvertToQueue(calculationStack);
            Queue<OperationType> operationsQueue = ConvertToQueue(operationsStack);


            while (calculationQueue.Count > 0)
            {
                int currentLoopItems = calculationQueue.Count;

                if (isEndToMultiplication)
                {
                    isTimeToSumQueue = true;
                }

                isEndToMultiplication = true;

                while (currentLoopItems > 0)
                {
                    currentLoopItems--;

                    OperationType currentOperator = operationsQueue.Dequeue();

                    if (currentOperator == OperationType.Multiplication || currentOperator == OperationType.Division)
                    {
                        float currentItem1 = calculationQueue.Dequeue();
                        float currentItem2 = calculationQueue.Dequeue();
                        float currentResult = CalculateOperation(currentOperator, currentItem2, currentItem1);

                        OperationType nextOperation = operationsQueue.Dequeue();

                        //add current value to next loop in the queue
                        calculationQueue.Enqueue(currentResult);
                        operationsQueue.Enqueue(nextOperation);

                        if (nextOperation == OperationType.Multiplication || nextOperation == OperationType.Division)
                        {
                            isEndToMultiplication = false;
                        }
                    }
                    else
                    {
                        //Set proper operation order
                        operationsQueue.Enqueue(currentOperator);
                        float currentItem = calculationQueue.Dequeue();
                        calculationQueue.Enqueue(currentItem);

                        if (isTimeToSumQueue)
                        {
                            while (calculationQueue.Count > 0)
                            {
                                OperationType operation = operationsQueue.Dequeue();

                                if (operation == OperationType.Subtraction)
                                {
                                    result -= calculationQueue.Dequeue();
                                }
                                else if (operation == OperationType.Addition ||
                                    operation == OperationType.None)
                                {
                                    result += calculationQueue.Dequeue();
                                }
                            }
                            break;
                        }
                    }
                }
            }

            LogTool.Log(LogDataType.Formula, $"Result {result}");

            return result;

        }
        private OperationType ConvertStringToOperation(char operationType) => operationType switch
        {
            '+' => OperationType.Addition,
            '-' => OperationType.Subtraction,
            '*' => OperationType.Multiplication,
            '/' => OperationType.Division,
            '^' => OperationType.Power,
            _ => NotImplementedValueInCalculate(operationType)
        };
        private float CalculateOperation(OperationType operationType, float item, float item2)
        {
            if (operationType == OperationType.Addition) return item + item2;
            else if (operationType == OperationType.Subtraction) return item - item2;
            else if (operationType == OperationType.Multiplication) return item * item2;
            else if (operationType == OperationType.Division) return item / item2;
            return 0;
        }
        public enum OperationType
        {
            Addition,
            Subtraction,
            Multiplication,
            Division,
            Power,
            None,
        }
        private OperationType NotImplementedValueInCalculate(char operationType)
        {
            LogTool.Log(LogDataType.Formula, $"Operator is not implemented: {operationType}");
            return OperationType.None;
        }
    }
    public class ItemFormulaData
    {
        public int itemLevelBase;
        public int itemLevelTotal;

        public ItemFormulaData(int itemLevelBase, int itemLevelTotal)
        {
            this.itemLevelBase = itemLevelBase;
            this.itemLevelTotal = itemLevelTotal;
        }
    }
}
