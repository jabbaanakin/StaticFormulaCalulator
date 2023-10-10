using System;
using System.ComponentModel.Design;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using StaticFormulaCalulator;


//Код отсюда можно игнорировать, тк здесь я инициилизирую псевдо данные для работы с DataTable
//Готовый же DataTable вы можете взять из таблицы dataComponents (или как там она называется)

DataColumn idColumn = new DataColumn("Id", Type.GetType("System.Int32")!);
DataColumn nameColumn = new DataColumn("Name", Type.GetType("System.String")!);
DataColumn valueColumn = new DataColumn("Value", Type.GetType("System.Decimal")!);
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
DataColumn varColumn = new DataColumn("ShortName", Type.GetType("System.String")!);
DataColumn fullColumn = new DataColumn("IDName", Type.GetType("System.String")!);
dt.Columns.Add(varColumn);
dt.Columns.Add(fullColumn);

for (int i = 0; i < dt.Rows.Count; i++)
{
    string? d = dt.Rows[i][0].ToString();
    string s = (string)dt.Rows[i][1];
    if (s.IndexOf('.') != -1)
        dt.Rows[i][3] = s[..s.IndexOf('.')];
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


ParserForStaticFormula pfdf = new();
string valuesFormula = pfdf.ParseForStaticFormula(ref formula, dt);

Console.WriteLine($"Формула с параметрами: {formula}\nФормула со значениями: {valuesFormula}");

Calculating calc = new();

decimal result = calc.Calculate(ref valuesFormula);

Console.WriteLine(result);