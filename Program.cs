using System;
using System.ComponentModel.Design;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;


//Код отсюда можно игнорировать, тк здесь я инициилизирую псевдо данные для работы с DataTable
//Готовый же DataTable вы можете взять из таблицы dataComponents (или как там она называется)

DataColumn idColumn = new DataColumn("Id", Type.GetType("System.Int32"));
DataColumn nameColumn = new DataColumn("Name", Type.GetType("System.String"));
DataColumn valueColumn = new DataColumn("Value", Type.GetType("System.Decimal"));
DataColumn[] columns = new DataColumn[3] { idColumn, nameColumn, valueColumn };

DataTable dt = new DataTable();
dt.Columns.AddRange(columns);

object[] r = { 1, "C1", 54.11m };
object[] r1 = { 2, "C2", 11.23m };
object[] r2 = { 3, "C2.1", 62.77m };
object[] r3 = { 4, "C3", 77.82m };
object[] r4 = { 5, "C4", 12.18m };
object[] r5 = { 6, "C5", 44.52m };

dt.Rows.Add(r);
dt.Rows.Add(r1);
dt.Rows.Add(r2);
dt.Rows.Add(r3);
dt.Rows.Add(r4);
dt.Rows.Add(r5);
//До сюда это лишь инициализация. Готовая версия у вас уже есть


//Здесь я добавляю в таблицу дополнительные колонки. Одна из этих колонок и так присутствует в программе
//В момент инициализации окошка, чтобы пользователь выбирал компоненты, которые ему нужны для расчета стоимости
//Сделано это было для группиовки компонентов, например: С2, С3 и тд.
//Здесь за это отвечает колонка ShortName
//IDName это колонка складывющая ID компонента и его название. Делается это для того, чтобы клиент при выборе компонентов
//с одинаковыми именами, но разными стоимостями (например С2 0.8квт и С2 6кВт) различаились при замене имен на стоимости для формулы
DataColumn varColumn = new DataColumn("ShortName", Type.GetType("System.String"));
DataColumn fullColumn = new DataColumn("IDName", Type.GetType("System.String"));
dt.Columns.Add(varColumn);
dt.Columns.Add(fullColumn);

for (int i = 0; i < dt.Rows.Count; i++)
{
    string d = dt.Rows[i][0].ToString();
    string s = (string)dt.Rows[i][1];
    if (s.IndexOf('.') != -1)
        dt.Rows[i][3] = s.Substring(0, s.IndexOf('.'));
    else dt.Rows[i][3] = s;

    dt.Rows[i][4] = d + s;
}

//Здесь формулы, которые я тестил. Можно провести дополнительные тесты с другими формулами.
//Пока что приложение рассчатано на операции складывания, вычитания, умножения и деления
//При выявлении ошибок просьба обратиться к программисту-первоисточнику либо же при простой ошибке решить ее самостоятельно
//В вашем же приложении, формула будет вытягиваться из базы данных из таблицы, которую будет заполнять клиент.
//Поэтому важно провести как можно больше тестов

//string formula = "C1+C2+C3+C4";
//string formula = "C1+(C2*C3)/(C4*C5)";
string formula = "C1*C2+C3*(C4/C5)";
//string formula = "(C1/(C2-C3))+(C4/(C2-C3))-C5*C2";
//string formula = "C1*C2-((C3*C4)/C5)";
//string formula = "C1+C2*(C3*C4/C5)";
//string formula = "(C1*C2*(C3-C4))/C5";

ParserForDinamycFormula pfdf = new(formula, dt);
Calculating calc = new (pfdf.FullFormula);

/// <summary>
/// Класс, предназначенный для того, чтобы поправить формулу для клиента, а также
/// сгенерировать формулу с подставленными значениями стоимости
/// </summary>
class ParserForDinamycFormula
{
    static string formula;
    static string fullFormula;

    public string Formula
    {
        get { return formula; }
        set { formula = value; }
    }

    public string FullFormula
    {
        get { return fullFormula; }
        set { fullFormula = value; }
    }

    /// <summary>
    /// Конструктор класса, соответственно в этом методе поправляется формула для клиента, а также 
    /// генерируется формула с подставленными значениями стоимости
    /// </summary>
    /// <param name="f">String формула</param>
    /// <param name="dt">DataTable таблица с набором компонентов</param>
    /// <returns>Записывает в свойства класса формулу для клиента и формулу для дальнейшего рассчета</returns>
    public ParserForDinamycFormula(string f, DataTable dt)
    {
        formula = f; fullFormula = f;
        string[] variables = dt.AsEnumerable().Select(row => row.Field<string>("ShortName")).ToArray();
        
        //Разделяем формулу на неизвестные компоненты и удаляем из формулы компоненты, 
        //которые не были выбраны клиентом
        //!!!ВАЖНО!!! В формуле не должно быть пробелов
        Regex regSplit = new Regex(@"\W");
        string[] result = regSplit.Split(formula);
        foreach (string res in result)
        {
            if (!variables.Contains(res) && res != "")
            {
                DeleteExcessComponents(res);
            }
        }

        //В этом блоке мы как раз и заменяем краткое название компонентов на полное
        List<string[]> variableArray = new List<string[]>();
        var groups = variables.GroupBy(v => v);
        foreach (var group in groups)
        {
            variableArray.Add(group.ToArray());
        }
        BuildCorrectFormulas(variableArray, dt);

        //Переставляем компоненты на значения для второй формулы, а в дальнейшем и для просчета
        for (int i = 0; i < dt.Rows.Count; i++)
        {
            fullFormula = fullFormula.Replace((string)dt.Rows[i][4], dt.Rows[i][2].ToString());
        }
    }

    /// <summary>
    /// Удаляет из формулы недостающие компоненты
    /// </summary>
    /// <param name="variables">string[] Массив компонентов</param>
    /// <returns>В будущем можно слегка переписать, чтобы возвращало строку либо две строки в string[]</returns>
    public void DeleteExcessComponents(string res)
    {
        if (formula[formula.IndexOf(res) - 1] == '(' && formula[formula.IndexOf(res) + 2] == ')')
        {
            formula = formula.Remove(formula.IndexOf(res) - 2, res.Length + 3);
            fullFormula = fullFormula.Remove(formula.IndexOf(res) - 2, res.Length + 3);
        }
        else if (formula[formula.IndexOf(res) - 1] == '(')
        {
            formula = formula.Remove(formula.IndexOf(res), res.Length + 1);
            fullFormula = fullFormula.Remove(fullFormula.IndexOf(res), res.Length + 1);
        }
        else
        {
            formula = formula.Remove(formula.IndexOf(res) - 1, res.Length + 1);
            fullFormula = fullFormula.Remove(fullFormula.IndexOf(res) - 1, res.Length + 1);
        }
    }

    /// <summary>
    /// Заменяет в формуле краткие значения на полные, а также заменяет компоненты на их значения
    /// </summary>
    /// <param name="variables">string[] Массив компонентов</param>
    /// <param name="dt">DataTable Таблица с данными</param>
    /// <returns>В будущем можно слегка переписать, чтобы возвращало строку либо две строки в string[]</returns>
    public void BuildCorrectFormulas(List<string[]> variableArray, DataTable dt)
    {
        foreach (string[] s in variableArray.ToArray())
        {
            DataRow[] dr = dt.Select($"ShortName = '{s[0]}'");
            if (s.Length > 1)
            {
                string replacingVarible = "(" + (string)dr[0][1];
                string replacingVaribleFull = "(" + (string)dr[0][4];
                for (int i = 1; i < s.Length; i++)
                {
                    replacingVarible += "+" + (string)dr[i][1];
                    replacingVaribleFull += "+" + (string)dr[i][4];
                }
                replacingVarible += ")";
                replacingVaribleFull += ")";
                Regex regularEx = new Regex($"{s[0]}+");
                if (regularEx.IsMatch(formula))
                {
                    formula = formula.Replace(s[0], replacingVarible);
                    fullFormula = fullFormula.Replace(s[0], replacingVaribleFull);
                }
            }
            else
            {
                string replacingVaribleFull = (string)dr[0][4];
                fullFormula = fullFormula.Replace(s[0], replacingVaribleFull);
            }

        }
    }
}

/// <summary>
/// Класс, предназначенный для рассчета готовой формулы
/// </summary>
class Calculating
{
    static string formula;

    public string Formula
    {
        get { return formula; }
        set { formula = value; }
    }

    /// <summary>
    /// Конструктор класса, который отправляет сначала операции в скобках,
    /// пока все скобки не будут открыты
    /// </summary>
    /// <param name="f">string Формула</param>
    public Calculating(string f)
    {
        formula = f;

        Console.WriteLine(formula);
        Console.WriteLine(f);

        bool deletable = true;

        //Пока все скобки не будут раскрыты, отправляем операции в скобках на решение
        while (deletable)
        {
            string ff = formula;
            if (ff.IndexOf(')') != -1)
            {
                string[] rfs = ff.Split(')');
                string[] lfs = rfs[0].Split('(');

                string temp = lfs[lfs.Length - 1];

                string t = CalculateEachValue(temp).ToString();

                formula = formula.Replace(String.Concat("(", temp, ")"), t);
            }
            else
            {
                deletable = false;
            }
        }

        //Итоговый расчет формулы с округлением до двух знаков после запятой
        decimal s = Math.Round(CalculateEachValue(formula), 2, MidpointRounding.AwayFromZero);
        Console.WriteLine(s);
        
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
                    while(ms.Count > 0) 
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
            while(lows.Count > 0)
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
                if (resulth < 0)
                {
                    Console.WriteLine("Во время расчета число получилось отрицательным. Для избежания ошибки просьба проверить данные.\n" +
                        $"Ошибка произошла при следующей операции: {lowVals[0]} {lows[0].Value} {lowVals[1]} = {resulth.ToString()}.\n" +
                        $"Для дальнейшего расчета результат операции изменен на 1");
                    resulth = 1;
                }
                bform = bform.Replace(String.Concat(lowVals[0], lows[0].Value, lowVals[1]), resulth.ToString());
                Console.WriteLine(bform);
            }
        }
        return Convert.ToDecimal(bform);
    }
}
