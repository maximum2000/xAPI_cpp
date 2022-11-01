using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class ReportStorageClass 
{
	[HideInInspector]
	public List<ReportStorageStepClass> ReportStorageStepsList = new List<ReportStorageStepClass>();

	[HideInInspector]
	public List<ReportStorageMathmodelValuesClass> ReportStorageMMValuesList = new List<ReportStorageMathmodelValuesClass>();

	[HideInInspector]
	public List<EffectStorageStepClass> ReportStorageEffextsList = new List<EffectStorageStepClass>();

	[HideInInspector]
	public List<ReportStorageInstructorClass> ReportStorageInstructorList = new List<ReportStorageInstructorClass>();

	//текущий id записи
	int id=0;
}






//--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
//Эталоны описания данных
//--------------------------------------------------------------------------------------------------------------------------------------------------------------------------

/*
//1. Описание утверждения о действиях обучаемого по сценарию
other: [
.....
{
	id: "http://lcontent.ru/xapi/step",
	definition: 
	{
		//id элемента отчета, порядковый номер
		name: 
		{
			ru-RU: "1"
		},
		description: 
		{
			ru-RU: "Вопрос:Что нужно выполнить?. Ответ пользователя: Проверить документацию (удостоверения членов вахты). Ответ неверен. Верный ответ: Проверить документацию (наряд-заказ, наряд-допуск на производство работ, акт допуск на работы повышенной опасности, удостоверения членов вахты, вахтовый журнал)"
		},
		datatime_real:
		{
			ru-RU: "2021-03-22 22:56:13"
		},
		datatime_simulation:
		{
			ru-RU: "2021-03-22 22:56:13"
		},
		//тип шага (вопрос, пилон, нажатие на объект, ожидание значения, показ текста и т.д. = типу шага сценария
		type:
		{
			ru-RU: "Задан вопрос"
		},
		//объем выполнения (0..1)
		completed:
		{
			ru-RU: 0.0
		},
		//точность/правильность выполнения 0..1
		passed:
		{
			ru-RU: 0.2
		},
		//категория "луча" (общее/соблюдение регламента/безопасность выполнения/Время выполнения/Точность выполнения/Безопасность выполнения/Обнаружение и диагностик и т.д.
		categoty:
		{
			ru-RU: соблюдение регламента
		}
	},
	objectType: "Activity"
},
*/
public class ReportStorageStepClass
{
	public string guid_id = ""; //guid System.Guid.NewGuid().ToString()
	//например Вопрос:Что нужно выполнить?. Ответ пользователя: Проверить документацию (удостоверения членов вахты). Ответ неверен.
	public string definition_description = "";
	//2021-03-22 22:56:13
	public string datatime_real = "";
	//2021-03-22 22:56:13
	public string datatime_simulation = "";
	//тип шага (вопрос, пилон, нажатие на объект, ожидание значения, показ текста и т.д. = типу шага сценария
	//ScenarioEditor.ExampleEnum
	public string type = "";
	//объем выполнения (0..1)
	public float completed = 0;
	//точность/правильность выполнения 0..1
	public float passed = 0;
	//категория "луча" (общее/соблюдение регламента/безопасность выполнения/Время выполнения/Точность выполнения/Безопасность выполнения/Обнаружение и диагностик и т.д.
	public string categoty = "";
}


/*
//2. Описание утверждения о параметре (ММ) модели

other: [
.....
{
	id: "http://lcontent.ru/xapi/mathmodel_value",
	definition: 
	{
		name: 
		{
			ru-RU: "2"
		},
		description: 
		{
			ru-RU: "Pump1.P"
		},

values: [
		datatime_real:
		{
			ru-RU: "2021-03-22 22:56:13"
		},
		datatime_simulation:
		{
			ru-RU: "2021-03-22 22:56:13"
		},
		//значение
		value:
		{
			ru-RU: "10.7342432234"
		},
]
		//размерность
		dimension:
		{
			ru-RU: "м3/мин"
		},
		
	},
	objectType: "Activity"
},
*/
//описание 1 записи
public class ReportStorageMathmodelValueClass
{
	//2021-03-22 22:56:13
	public string datatime_real = "";
	//2021-03-22 22:56:13
	public string datatime_simulation = "";
	//значение ММ (число/строка)
	public string value = "";
}
//описание набора записей по одной переменной
public class ReportStorageMathmodelValuesClass
{
	public string id = "http://lcontent.ru/xapi/mathmodel_values";

	//id элемента отчета, порядковый номер записи
	public int definition_name = 0;
	//имя переменной, например Pump1.P
	public string definition_description = "";
	//размерность,например м3/мин
	public string dimension = "";

	[HideInInspector]
	public List<ReportStorageMathmodelValueClass> values = new List<ReportStorageMathmodelValueClass>();
}

/*
//3. Описание утверждения о потерях / последствиях

other: [
.....
{
	id: "http://lcontent.ru/xapi/losses",  или /effects
	definition: 
	{
		//id последствия
		name: 
		{
			ru-RU: "3"
		},
		//ссылка или "", ссылка на id причины для построения диаграмм причинно-следственных связей (ETA/FTA)
		ref_parent: 
		{
			ru-RU: "4"
		},
		//ссылка или "", ссылка на id альтернативной причины, ну например, если-бы пользователь нажал верную кнопку
		ref_alternative_parent: 
		{
			ru-RU: "5"
		},
		description: 
		{
			ru-RU: "кавитационное разрушение насоса Д1"
		},
		//время/дата
		datatime_real:
		{
			ru-RU: "2021-03-22 22:56:13"
		},
		datatime_simulation:
		{
			ru-RU: "2021-03-22 22:56:13"
		},
		//причина коротко
		cause:
		{
			ru-RU: "не определена/ошибка восприятия/диагностики/принятия решения/выполнение действий"
		},
		//причина полный
		cause_full:
		{
			ru-RU: "персонал стремился к выполнению неверной цели, персонал не увидел первых признаков ГНВП, персонал слишком долго принимал решение"
		},
		//потери общее
		losses:
		{
			ru-RU: "Выход из строя насоса Д1"
		},
		losses_money:
		{
			ru-RU: "100"
		},
		losses_life_health:
		{
			ru-RU: "сломана нога, 1 погиб, 1 находится в коме"
		},
		losses_ecology:
		{
			ru-RU: "розлив нефти в количестве 10 тонн"
		},
		
	},
	objectType: "Activity"
},
*/
public class EffectStorageStepClass
{
	//public string id = "http://lcontent.ru/xapi/effects";

	//id элемента отчета, порядковый номер
	public string guid_id = ""; //guid System.Guid.NewGuid().ToString()
	//ссылка или "", ссылка на id причины для построения диаграмм причинно-следственных связей (ETA/FTA)
	public string ref_parent = "";
	//например кавитационное разрушение насоса Д1.
	public string definition_description = "";
	//2021-03-22 22:56:13
	public string datatime_real = "";
	//2021-03-22 22:56:13
	public string datatime_simulation = "";
	//причина коротко, не определена/ошибка восприятия/диагностики/принятия решения/выполнение действий
	public string cause = "";
	//причина причина полный, персонал стремился к выполнению неверной цели, персонал не увидел первых признаков ГНВП, персонал слишком долго принимал решение
	public string cause_full = "";
	//потери общее, Выход из строя насоса Д1
	public string losses ="";
	//потери $
	public float losses_money = 0;
	//потери жизни и здоровья, сломана нога, 1 погиб, 1 находится в коме
	public float losses_life_health = 0;
	//потери экология, розлив нефти в количестве 10 тонн
	public float losses_ecology = 0;
}


/*
//4.Параметры и неисправности, заданные иснтруктором
other: [
.....
{
	id: "http://lcontent.ru/xapi/instructor_values",
	definition: 
	{
		name: 
		{
			ru-RU: "2"
		},
		description: 
		{
			ru-RU: "Сценарий №1. Запуск СК в условиях -40"
		},
		//может быть список сделать типа values [dtatatime/value]
		parametrs:
		[
			name:
			{
				ru-RU: "Насос1.Количество ступеней"
			},
			datatime_real:
			{
				ru-RU: "2021-03-22 22:56:13"
			},
			datatime_simulation:
			{
				ru-RU: "2021-03-22 22:56:13"
			},
			//значение
			value:
			{
				ru-RU: "34"
			},
			//размерность
			dimension:
			{
				ru-RU: "шт."
			},
		],
		problems:
		[
			name:
			{
				ru-RU: "Задвижка3.Клин"
			},
			datatime_real:
			{
				ru-RU: "2021-03-22 22:56:13"
			},
			datatime_simulation:
			{
				ru-RU: "2021-03-22 22:56:13"
			},
			//значение
			value:
			{
				ru-RU: "да"
			},
			//размерность
			dimension:
			{
				ru-RU: ""
			},
		],
		//комментарии инструктора
		comments:
		[
			datatime_real:
			{
				ru-RU: "2021-03-22 22:56:13"
			},
			datatime_simulation:
			{
				ru-RU: "2021-03-22 22:56:13"
			},
			comment:
			{
				ru-RU: "Грубое нарушение техники безопасности."
			}
		]
		
	},
	objectType: "Activity"
},
*/
//описание 1 параметра
public class ReportStorageInstructorParameterClass
{
	//Насос1.Количество ступеней
	public string name = "";
	//2021-03-22 22:56:13
	public string datatime_real = "";
	//2021-03-22 22:56:13
	public string datatime_simulation = "";
	//значение (число/строка)
	public string value = "";
	//размерность
	public string dimension = "";
}

//описание 1 введенной неисправности
public class ReportStorageInstructorProblemClass
{
	//Задвижка3.Клин
	public string name = "";
	//2021-03-22 22:56:13
	public string datatime_real = "";
	//2021-03-22 22:56:13
	public string datatime_simulation = "";
	//значение (число/строка), да
	public string value = "";
}

//описание 1 комментария
public class ReportStorageInstructorCommentClass
{
	//2021-03-22 22:56:13
	public string datatime_real = "";
	//2021-03-22 22:56:13
	public string datatime_simulation = "";
	//комментарий
	public string comment = "";
}

//описание набора записей по инструктору
public class ReportStorageInstructorClass
{
	public string id = "http://lcontent.ru/xapi/instructor_values";

	//id элемента отчета, порядковый номер записи
	public int definition_name = 0;
	//имя сценария или состояния первоначально взятого за основу, Сценарий №1. Запуск СК в условиях -40
	public string definition_description = "";

	[HideInInspector]
	public List<ReportStorageInstructorParameterClass> parametrs = new List<ReportStorageInstructorParameterClass>();
	[HideInInspector]
	public List<ReportStorageInstructorProblemClass> problems = new List<ReportStorageInstructorProblemClass>();
	[HideInInspector]
	public List<ReportStorageInstructorCommentClass> comments = new List<ReportStorageInstructorCommentClass>();
}


/*
//5.Лог нейроинтерфейса
other: [
.....
{
	id: "http://lcontent.ru/xapi/neurointerface_log",  
	definition: 
	{
		//id 
		name: 
		{
			ru-RU: "3"
		},
		description: 
		{
			ru-RU: "лог нейроинтерфейса"
		},
		//время/дата
		datatime_real:
		{
			ru-RU: "2021-03-22 22:56:13"
		},
		datatime_simulation:
		{
			ru-RU: "2021-03-22 22:56:13"
		},
		//причина коротко
		model:
		{
			ru-RU: "модель устройства"
		},
		log:[
			[50,10,5,50],
            [51,9,6.5,58.5],
            [52,8,7,56],
            [53,7,8.5,59.5]
            ]
	},
	objectType: "Activity"
},
*/
