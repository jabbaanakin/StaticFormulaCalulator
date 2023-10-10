using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticFormulaCalulator
{
    /// <summary>
    /// Класс, предназначенный для того, чтобы поправить формулу для клиента, а также
    /// сгенерировать формулу с подставленными значениями стоимости
    /// </summary>
    internal class ParserForStaticFormula
    {
        public ParserForStaticFormula()
        {

        }

        /// <summary>
        /// Конструктор класса, соответственно в этом методе поправляется формула для клиента, а также 
        /// генерируется формула с подставленными значениями стоимости
        /// </summary>
        /// <param name="f">String формула</param>
        /// <param name="dt">DataTable таблица с набором компонентов</param>
        /// <returns>Записывает в свойства класса формулу для клиента и формулу для дальнейшего рассчета</returns>
        public string ParseForStaticFormula(ref string f, DataTable dt)
        {
            string formula = f; //Формула, в которую подставим числовые значения

            string[] variables = dt.AsEnumerable().Select(row => row.Field<string>("ShortName")!).ToArray();

            //Разделяем формулу на неизвестные компоненты и удаляем из формулы компоненты, 
            //которые не были выбраны клиентом
            //!!!ВАЖНО!!! В формуле не должно быть пробелов
            Regex regSplit = new Regex(@"\W");
            string[] result = regSplit.Split(formula);
            foreach (string res in result)
            {
                if (!variables.Contains(res) && res != "")
                {
                    DeleteExcessComponents(res, formula);
                    DeleteExcessComponents(res, f);
                }
            }

            //В этом блоке мы как раз и заменяем краткое название компонентов на полное
            List<string[]> variableArray = new List<string[]>();
            var groups = variables.GroupBy(v => v);
            foreach (var group in groups)
            {
                variableArray.Add(group.ToArray());
            }

            BuildCorrectFormulas(variableArray, dt, ref f,  1); 
            BuildCorrectFormulas(variableArray, dt, ref formula, 4);

            //Переставляем компоненты на значения для второй формулы, а в дальнейшем и для просчета
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                formula = formula.Replace((string)dt.Rows[i][4], dt.Rows[i][2].ToString());
            }

            return formula;
        }

        /// <summary>
        /// Удаляет из формулы недостающие компоненты
        /// </summary>
        /// <param name="variables">string[] Массив компонентов</param>
        /// <param name="formula">string Формула</param>
        /// <returns>В будущем можно слегка переписать, чтобы возвращало строку либо две строки в string[]</returns>
        public void DeleteExcessComponents(string res, string formula)
        {
            if (formula[formula.IndexOf(res) - 1] == '(' && formula[formula.IndexOf(res) + 2] == ')')
            {
                formula = formula.Remove(formula.IndexOf(res) - 2, res.Length + 3);
            }
            else if (formula[formula.IndexOf(res) - 1] == '(')
            {
                formula = formula.Remove(formula.IndexOf(res), res.Length + 1);
            }
            else
            {
                formula = formula.Remove(formula.IndexOf(res) - 1, res.Length + 1);
            }
        }

        /// <summary>
        /// Заменяет в формуле краткие значения на полные, а также заменяет компоненты на их значения
        /// </summary>
        /// <param name="variables">string[] Массив компонентов</param>
        /// <param name="dt">DataTable Таблица с данными</param>
        /// <param name="formula">DataTable Формула</param>
        /// <param name="index">int Колонка, с которой стоит брать значения</param>
        /// <returns>Возвращает готовую формулу с значениями</returns>
        public void BuildCorrectFormulas(List<string[]> variableArray, DataTable dt, ref string formula, int index)
        {
            foreach (string[] s in variableArray.ToArray())
            {
                DataRow[] dr = dt.Select($"ShortName = '{s[0]}'");
                if (s.Length > 1)
                {
                    string replacingVarible = "(" + (string)dr[0][index];
                    for (int i = 1; i < s.Length; i++)
                    {
                        replacingVarible += "+" + (string)dr[i][index];
                    }
                    replacingVarible += ")";
                    Regex regularEx = new Regex($"{s[0]}+");
                    if (regularEx.IsMatch(formula))
                    {
                        formula = formula.Replace(s[0], replacingVarible);
                    }
                }
                else if(index == 4)
                {
                    string replacingVaribleFull = (string)dr[0][index];
                    formula = formula.Replace(s[0], replacingVaribleFull);
                }
            }
        }

    }
}
