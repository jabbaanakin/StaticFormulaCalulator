using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StaticFormulaCalulator
{
    /// <summary>
    /// Класс, предназначенный для рассчета готовой формулы
    /// </summary>
    class Calculating
    {

        public Calculating()
        {

        }

        /// <summary>
        /// Метод, который отправляет сначала операции в скобках,
        /// пока все скобки не будут открыты
        /// </summary>
        /// <param name="f">string Формула</param>
        public decimal Calculate(ref string formula)
        {
            bool deletable = true;

            //Пока все скобки не будут раскрыты, отправляем операции в скобках на решение
            while (deletable)
            {
                string ff = formula;
                if (ff.IndexOf(')') != -1)
                {
                    string[] rfs = ff.Split(')');
                    string[] lfs = rfs[0].Split('(');

                    string temp = lfs[^1];

                    string t = CalculateEachValue(temp).ToString();

                    formula = formula.Replace(String.Concat("(", temp, ")"), t);
                }
                else
                {
                    deletable = false;
                }
            }

            //Итоговый расчет формулы с округлением до двух знаков после запятой
            return Math.Round(CalculateEachValue(formula), 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Рассчитывает формулу
        /// </summary>
        /// <param name="bform">string формула(либо полная, либо в скобках)</param>
        /// <returns>decimal result</returns>
        private decimal CalculateEachValue(string bform)
        {
            Regex regHighProir = new Regex(@"\*|\/");

            Regex regLowProir = new Regex(@"\+|\-");
            string[] lowVals = regLowProir.Split(bform);

            MatchCollection lows = regLowProir.Matches(bform);
            MatchCollection highs = regHighProir.Matches(bform);

            if (highs.Count > 0 || lows.Count > 0)
            {
                //Сначала рассчитываем операции умножения или деления (высокий приоритет)
                for (int i = 0; i < lowVals.Length; i++)
                {
                    if (regHighProir.IsMatch(lowVals[i]))
                    {
                        decimal result = 0m;
                        //Разделяем формулу на под формулы благодаря менее приоритетным операциям +, -
                        // Предположим у нас формула x-a*b, когда мы разделим по low priority operation (смотря какой fabric)
                        //то получим x, a*b. И в данном блоке как раз и происходит умножение
                        MatchCollection ms = regHighProir.Matches(lowVals[i]);
                        while (ms.Count > 0)
                        {
                            lowVals = regLowProir.Split(bform);
                            string[] hh = regHighProir.Split(lowVals[i]);
                            ms = regHighProir.Matches(lowVals[i]);
                            if (ms.Count <= 0)
                                break;
                            switch (ms[0].Value)
                            {
                                case "*":
                                    {
                                        result = Convert.ToDecimal(hh[0]) * Convert.ToDecimal(hh[1]);
                                        break;
                                    }
                                case "/":
                                    {
                                        result = Convert.ToDecimal(hh[0]) / Convert.ToDecimal(hh[1]);
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }

                            bform = bform.Replace(String.Concat(hh[0], ms[0].Value, hh[1]), result.ToString());
                        }
                    }
                }
                decimal resulth = 1;
                lows = regLowProir.Matches(bform);
                while (lows.Count > 0)
                {
                    //После того, как рассчитаны операции с высоким приориетом, мы просчитываем операции с низким приоритетом
                    //В случае, если результат у нас выходит отрицательным (Например, -3,14), то выдаем ошибку, что данные неверны
                    //Так как в случае с деньгами и услугами, компания ничего не должна клиенту денег
                    lowVals = regLowProir.Split(bform);
                    lows = regLowProir.Matches(bform);
                    if (lows.Count <= 0)
                        break;
                    switch (lows[0].Value)
                    {
                        case "+":
                            {
                                resulth = Convert.ToDecimal(lowVals[0]) + Convert.ToDecimal(lowVals[1]);
                                break;
                            }
                        case "-":
                            {
                                resulth = Convert.ToDecimal(lowVals[0]) - Convert.ToDecimal(lowVals[1]);
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                    if (resulth <= 0)
                    {
                        Console.WriteLine("Во время расчета число получилось отрицательным. Для избежания ошибки просьба проверить данные.\n" +
                            $"Ошибка произошла при следующей операции: {lowVals[0]} {lows[0].Value} {lowVals[1]} = {resulth}.\n" +
                            $"Для дальнейшего расчета результат операции изменен на 1");
                        resulth = 1;
                    }
                    bform = bform.Replace(String.Concat(lowVals[0], lows[0].Value, lowVals[1]), resulth.ToString());
                }
            }
            return Convert.ToDecimal(bform);
        }
    }
}
