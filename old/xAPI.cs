/***************************************************************************
xAPI.cs  - модуль взаимодействия по стандарту xAPI/TinCAN/CMI5 
-------------------
begin                : 21 01 2021
copyright            : (C) 2021 by Гаммер Максим Дмитриевич (maximum2000)
email                : MaxGammer@gmail.com
site				 : lcontent.ru 
org					 : Гаммер Максим Дмитриевич
***************************************************************************/

//https://stackoverflow.com/questions/61946617/how-to-call-external-javascript-function-from-jslib-plugin-unity-webgl
//https://stackoverflow.com/questions/58157037/unity-webgl-fails-when-making-a-httpwebrequest-posthttps://stackoverflow.com/questions/58157037/unity-webgl-fails-when-making-a-httpwebrequest-post


//https://cloud.scorm.com/
//https://aicc.github.io/CMI-5_Spec_Current/client/


//https://stackoverflow.com/questions/224453/decrypt-php-encrypted-string-in-c-sharp
using System.Security.Cryptography;


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Net.NetworkInformation;
using System.IO;
using UnityEngine.SceneManagement;

using System.Net;
using System.Net.Sockets;
using System.Web;

using TMPro;

#if !MM
using Valve.VR;
#endif

#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

public class xAPI : MonoBehaviour 
{
	private IEnumerator coroutine_check;
	private IEnumerator coroutine_get;
	private IEnumerator coroutine_initialized;
	private IEnumerator coroutine_completed;
	private IEnumerator coroutine_passed;
	private IEnumerator coroutine_terminated;

	private IEnumerator coroutine_postactivation;

	[Header("Защита активна")]
	public bool ShieldOn;

	[Header("Защита")]
	public Text alertText;
	public GameObject CanvasShield;
	public InputField activationtext;

	[Header("Вывод подробного отчета / debug")]
	public TextMeshProUGUI t4;
	public GameObject CanvasDebug;

	[Header("О программе")]
	public GameObject CanvasAbout;
	public TextMeshProUGUI NameText;
	public TextMeshProUGUI LicenseText;

	[Header("Демо-режим")]
	public GameObject CanvasDemo;
	public GameObject CanvasDemoTimeOverPanel;
	float TargetTime = 5f * 60f;
	float MyTime = 0;

	[Header("Поле ФИО")]
	public TextMeshProUGUI FIO_Text=null;


	private string support_text = "По техническим вопросам и вопросам активации программного обеспечения обращайтесь по адресу https://lms.lcontent.ru/ в раздел техническая поддержка, либо отправьте Ваше сообщение на электронную почту support@ranik.org. Обязательно сообщите Ваши контакты и суть проблемы, приложите также необходимые скриншоты. Прямая ссылка - https://clck.ru/UD8RG";

	[HideInInspector]
	public string Registration=""; //cmi5
	[HideInInspector]
	public string endpoint;
	[HideInInspector]
	public string auth;
	[HideInInspector]
	public string numberScenario; //номер сценария
	[HideInInspector]
	public string HostOrOrganization = "";

	[HideInInspector]
	public float score = 99f;
	[HideInInspector]
	public float min = 0;
	[HideInInspector]
	public float max = 100f;
	[HideInInspector]
	public bool completion = true;
	[HideInInspector]
	public bool success = true;
	[HideInInspector]
	public bool examMode = true;

	//[HideInInspector]
	//public string report = "report detailed на русском";

	[Header("object.definition.name - значение используется если нет в launched")]
	public string myName = "";
	[Header("object.definition.description - значение используется если нет в launched")]
	public string myDiscription = "";


	private string _actor_mbox;
	private string _actor_name;
	private string _object_id;
	private string _context_registration;
	private string _sessionid;
	private string _actor_account_homePage;
	private string _actor_account_name;
	// 
	private string _context_contextActivities_grouping_id;
	private string _context_contextActivities_parent_id;
	//
	private CookieContainer cookieContainer;


	//меню запуска сценариев для скрытия/показа если указан параметр при запуске сценария
	[Header("меню запуска сценариев для скрытия/показа если указан параметр при запуске сценария")]
	public List <GameObject> ScenarionsGUI = new List<GameObject>();

	//общий ReportStorage
	[HideInInspector]
	public ReportStorageClass ReportStorage = new ReportStorageClass();


	private System.DateTime StartTime;
	private int secounds=0;


	string Lms_Host= "https://lms.lcontent.ru";



	private GetExternalIPAddress GetGeo;

	//FPS
	float qty = 1f;
	float currentAvgFPS = 30f;

#if UNITY_WEBGL
	[DllImport("__Internal")]
	private static extern void Loaded();

	[DllImport("__Internal")]
    private static extern void PostJsonToLRS(string str);
#endif


	//запускаем публикацию лицензии в LRS	
	public void PostActivation()
    {
		//запускаем
		coroutine_postactivation = PostActivationToLRS();
		StartCoroutine(coroutine_postactivation);
	}
	private IEnumerator PostActivationToLRS()
	{
		var secret = "Ty63rs4aVqcnh2vUqRJTbNT26caRZJ";
		//var encrypted = "OHnL0HoNKdpFXC9QoMTJ3o2G1rUO5aLzTraliB6oZ5oB1KEtSh/wxFABz5vxtpjJ";
		var DecryptedText = EncryptDecrypt.OpenSSLDecrypt(activationtext.text, secret);

		if (t4 != null) t4.text += System.Environment.NewLine + "POST activation" + System.Environment.NewLine;
		string _endpoint = endpoint + "statements";

		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(_endpoint);
		//UnityWebRequest httpWebRequest = (UnityWebRequest)WebRequest.Create(_endpoint);
		
		httpWebRequest.ContentType = "application/json; charset=UTF-8";
		httpWebRequest.Method = "POST";
		httpWebRequest.CookieContainer = cookieContainer;
		ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
		string autorization = auth;
		httpWebRequest.Headers.Add("Authorization", autorization);
		httpWebRequest.Headers.Add("X-Experience-API-Version", "1.0.0"); //.1

		//генерация JSON
		SimpleJSON.JSONNode rootNode = SimpleJSON.JSON.Parse(DecryptedText);
		if (rootNode["statements"][0]["actor"]["account"]["homePage"].IsString == true)
		{
			activationtext.text = "Корректный ключ. Информация извлечена ";
		}
		else
        {
			activationtext.text = "Некорректный ключ. Не удалось извечь информацию.";
			yield break;
		}
		//Конец генерация JSON

		byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(rootNode.ToString());
		if (t4 != null) t4.text += System.Environment.NewLine + System.Text.Encoding.UTF8.GetString(jsonData);
		
		httpWebRequest.ContentLength = jsonData.Length;
		using (var streamWriter = httpWebRequest.GetRequestStream())
		{
			streamWriter.Write(jsonData, 0, jsonData.Length);
			streamWriter.Flush();
			streamWriter.Close();
		}

		try
		{
			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var result = streamReader.ReadToEnd();
				Debug.Log("POST result" + result);
				if (t4 != null) t4.text += System.Environment.NewLine + "POST result" + System.Environment.NewLine + result;
				activationtext.text += "и отправлена.";
			}
		}
		catch (WebException ex)
		{

			if (ex.Response != null)
			{
				Debug.Log(ex.Message);
				if (t4 != null) t4.text += ex.Message;
				foreach (DictionaryEntry d in ex.Data)
				{
					Debug.Log(d.ToString());
					if (t4 != null) t4.text += d.ToString();
				}

				string errorDetail = string.Empty;
				using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream(), true))
				{
					errorDetail = streamReader.ReadToEnd();
					Debug.Log(errorDetail);
					if (t4 != null) t4.text += errorDetail;
				}
			}
		}
		yield return new WaitForSeconds(1f);
	}

	float UpdateCumulativeMovingAverageFPS(float newFPS)
	{
		qty += 0.1f;
		currentAvgFPS += (newFPS - currentAvgFPS) / qty;
		return currentAvgFPS;
	}

	//FPS, обработчик клавиши F8
	void Update()
	{
		float CurrentFPS = 1f / Time.deltaTime;
		UpdateCumulativeMovingAverageFPS(CurrentFPS);

		if ((Input.GetKeyDown(KeyCode.F8)) && (Input.GetKey(KeyCode.LeftShift)))
        {
			if (CanvasDebug == null) return;
			CanvasDebug.SetActive(!CanvasDebug.activeSelf);
        }

		if ((Input.GetKeyDown(KeyCode.F1) == true) && (Input.GetKey(KeyCode.LeftShift)))
		{
			if (CanvasAbout == null) return;
			CanvasAbout.SetActive(!CanvasAbout.activeSelf);
		}

		if ((CanvasDemo != null)&&(CanvasDemoTimeOverPanel != null))
		{
			if ((CanvasDemo.activeSelf == true) && (CanvasDemoTimeOverPanel.activeSelf == false))
			{
				MyTime += Time.deltaTime;
				if (MyTime > TargetTime)
				{
					CanvasDemoTimeOverPanel.SetActive(true);
				}
			}
		}


	}

	//функция добавляет учебные записи в общую базу записей
	public void AppendDataToReport(ReportStorageClass one)
    {
		//переписываю все шаги
		for (int i=0;i< one.ReportStorageStepsList.Count; i++)
        {
			ReportStorage.ReportStorageStepsList.Add(one.ReportStorageStepsList[i]);
		}

		//переписываю все ММ
		for (int i = 0; i < one.ReportStorageMMValuesList.Count; i++)
		{
			ReportStorage.ReportStorageMMValuesList.Add(one.ReportStorageMMValuesList[i]);
		}

		//переписываю все последствия
		for (int i = 0; i < one.ReportStorageEffextsList.Count; i++)
		{
			ReportStorage.ReportStorageEffextsList.Add(one.ReportStorageEffextsList[i]);
		}

		//переписываю все инструкторские
		for (int i = 0; i < one.ReportStorageInstructorList.Count; i++)
		{
			ReportStorage.ReportStorageInstructorList.Add(one.ReportStorageInstructorList[i]);
		}

	}

#if UNITY_WEBGL
	//Функция сообщает в JS что тренажер загрузился и JS может вызывать методы
	public static void IsLoaded()
	{
		Loaded();
	}
#endif


	//при запуске
	void Start()
	{
		//фиксируем время запуска
		StartTime = System.DateTime.Now;
		numberScenario = "0";

		//создаем регистратор геоданных
		GetGeo = gameObject.AddComponent<GetExternalIPAddress>();


#if !UNITY_WEBGL
		//если запущено из редактора
		if (Application.isEditor == true)
		{
			Registration = System.Guid.NewGuid().ToString();
			endpoint = "https://lrs.lcontent.ru/";
			auth = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("2eUGHaH3dEu7vkURXyQjEpKmidb46IM2qLPM5sRM" + ":" + "To3QouwxBNwjt6xQosvP18mm6qeslf7WDXNSEagd"));
			HostOrOrganization = "ООО ИНК";
		}
		else
		{
			string[] args = System.Environment.GetCommandLineArgs();
			
			Registration = "";
			endpoint = "";
			auth = "";
			
			HostOrOrganization = "";

			
			if (args.Length > 3)
			{
				Registration = WWW.UnEscapeURL(args[1]);
				endpoint = WWW.UnEscapeURL(args[2]);
				auth = WWW.UnEscapeURL(args[3]);
			}
			if (args.Length > 5)
			{
				numberScenario = WWW.UnEscapeURL(args[4]);
				HostOrOrganization = WWW.UnEscapeURL(args[5]);
			}

			if (t4 != null) t4.text += System.Environment.NewLine + "Registration=" + Registration + System.Environment.NewLine;
			if (t4 != null) t4.text += System.Environment.NewLine + "endpoint=" + endpoint + System.Environment.NewLine;
			if (t4 != null) t4.text += System.Environment.NewLine + "auth=" + auth + System.Environment.NewLine;
			if (t4 != null) t4.text += System.Environment.NewLine + "numberScenario=" + numberScenario + System.Environment.NewLine;
			if (t4 != null) t4.text += System.Environment.NewLine + "HostOrOrganization=" + HostOrOrganization + System.Environment.NewLine;

			if (ShieldOn == true)
			{
				if ((HostOrOrganization == "") || (Registration == "") || (endpoint == "") || (auth == ""))
				{
					CanvasShield.SetActive(true);
					if (alertText != null) alertText.text += "Ошибка проверки лицензии (нет параметров) .. ок" + System.Environment.NewLine + support_text;
					return;
				}
			}
		}

		//определение номера сценария
		int parsedInt = 0;
		if (int.TryParse(numberScenario, out parsedInt))
		{
			if (parsedInt > 0)
			{
				for (int i=0;i< ScenarionsGUI.Count;i++)
                {
					ScenarionsGUI[i].SetActive(false);
				}
				ScenarionsGUI[parsedInt-1].SetActive(true);
			}
		}

		//запрашиваем данные по стэйтменту Launcded (формируется до запуска тренажера)
		GetData();
	#endif


	#if UNITY_WEBGL
		IsLoaded();
	#endif
	}

	//отправка всех данных в LRS
	public void CommitToLRS() 
	{
		System.TimeSpan duration = System.DateTime.Now - StartTime;
		secounds = (int)duration.TotalSeconds;

		//https://github.com/adlnet/MasterObjectModel/blob/master/MOM_Spec.md#Initialized
		//Initialized
		//id: "https://adlnet.gov/expapi/verbs/initialized", display: "initialized",
		//definition: "Indicates that the activity was started."

		//step
		//URI: http://id.tincanapi.com/activitytype/step
		//A step is one of several actions that the actor has to do to achieve something, for instance, a goal or the completion of a task.For instance, a method, strategy or task could be divided into smaller steps.

		//Completed
		//id: "https://adlnet.gov/expapi/verbs/completed", display: "completed",
		//definition: "Indicates the actor finished or concluded the activity normally"
		//Result:
		//Success: RECOMMENDED
		//Duration: RECOMMENDED

		//Passed or Failed
		//id: "https://adlnet.gov/expapi/verbs/passed", display: "passed",
		//definition: "Indicates the actor completed an activity to standard"
		//Result:
		//Scaled:: RECOMMENDED
		//Success: TRUE
		//Completion: TRUE

		//Terminated
		//id: "https://adlnet.gov/expapi/verbs/terminated", display: "terminated",
		//definition: "Indicates the actor has completed their session normally"
		//Duration: RECOMMENDED


		if (Registration != "")
		{
			StartPostToLRS();
		}
	}

	//запись в LRS утверждения "initialized"
	private void StartPostToLRS()
	{
		//запускаем
		coroutine_initialized = PostToLRS_initialized();
		StartCoroutine(coroutine_initialized);
	}
	private IEnumerator PostToLRS_initialized()
	{
		if (t4 != null) t4.text += System.Environment.NewLine + "POST initialized" + System.Environment.NewLine;

		#if !UNITY_WEBGL
		string _endpoint = endpoint + "statements";

		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(_endpoint);
		httpWebRequest.ContentType = "application/json; charset=UTF-8";
		httpWebRequest.Method = "POST";
		httpWebRequest.CookieContainer = cookieContainer;
		ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

		string autorization = auth;
		httpWebRequest.Headers.Add("Authorization", autorization);
		httpWebRequest.Headers.Add("X-Experience-API-Version", "1.0.0"); //.1

		#endif

		//генерация JSON
		SimpleJSON.JSONNode rootNode = SimpleJSON.JSON.Parse("{}");

		if (_actor_mbox!="") rootNode["actor"]["mbox"] = _actor_mbox;
		if (_actor_account_homePage != "") rootNode["actor"]["account"]["homePage"] = _actor_account_homePage;
		if (_actor_account_name != "") rootNode["actor"]["account"]["name"] = _actor_account_name;

		rootNode["actor"]["name"] = _actor_name; // "Maxim Gammer";
		rootNode["actor"]["objectType"] = "Agent";
		//
		rootNode["verb"]["id"] = "http://adlnet.gov/expapi/verbs/initialized";    
		rootNode["verb"]["display"]["en-US"] = "Indicates that the activity was started.";
		rootNode["verb"]["display"]["ru-RU"] = "Указывает, что действие было начато.";
		//
		rootNode["object"]["id"] = _object_id;//myID;
		rootNode["object"]["objectType"] = "Activity";
		rootNode["object"]["definition"]["name"]["ru-RU"] = myName;
		rootNode["object"]["definition"]["description"]["ru-RU"] = myDiscription;
		//
		rootNode["context"]["registration"] = _context_registration; //Registration; 
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/sessionid"] = _sessionid; // Registration;  //!!!!!!!!!!!!!
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/masteryscore"] = 0.86f;
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/launchurl"] = "https://lcontent.ru";
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/launchmode"] = "Normal";

		//for cmi5
		SimpleJSON.JSONArray contextActivities_category = new SimpleJSON.JSONArray();
		//1
		{
			SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
			act1["objectType"] = "Activity";
			act1["id"] = "https://w3id.org/xapi/cmi5/context/categories/cmi5";
			contextActivities_category.Add(act1);
		}
		//
		rootNode["context"]["contextActivities"]["category"] = contextActivities_category;
		//
		if (_context_contextActivities_grouping_id != "")
		{
			SimpleJSON.JSONArray contextActivities_grouping = new SimpleJSON.JSONArray();
			//1
			{
				SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
				act1["objectType"] = "Activity";
				act1["id"] = _context_contextActivities_grouping_id; //"http://course-repository.example.edu/identifiers/courses/02baafcf/aus/4c07";
				contextActivities_grouping.Add(act1);
			}
			rootNode["context"]["contextActivities"]["grouping"] = contextActivities_grouping;
		}
		if (_context_contextActivities_parent_id != "")
		{
			SimpleJSON.JSONArray contextActivities_parent = new SimpleJSON.JSONArray();
			//1
			{
				SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
				act1["objectType"] = "Activity";
				act1["id"] = _context_contextActivities_parent_id; // "http://course-repository.example.edu/identifiers/courses/02baafcf/aus/4c07";
				contextActivities_parent.Add(act1);
			}
			rootNode["context"]["contextActivities"]["parent"] = contextActivities_parent;
		}
		//for cmi5

		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/weatherConditions"] = "rainy";
		rootNode["context"]["extensions"]["https://w3id.org/xapi/acme/extensions/training-location"] = GetGeo.city;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/geo-location"] = GetGeo.geolocation;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/externalip"] = GetGeo.externalip;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/Device"] = "HTC VIVE";
		rootNode["context"]["platform"] = "lms.lcontent.ru";
		//
		rootNode["context"]["instructor"]["objectType"] = "Agent";
		rootNode["context"]["instructor"]["mbox"] = "mailto:MaxGammer@gmail.com";
		rootNode["context"]["instructor"]["name"] = "Anna Gammer";
		rootNode["context"]["team"]["objectType"] = "Group";
		rootNode["context"]["team"]["mbox"] = "mailto:group@gmail.com";
		rootNode["context"]["team"]["name"] = "Commander ken";
		//Конец генерация JSON

		#if UNITY_WEBGL
		PostJsonToLRS(rootNode.ToString());
		#endif

#if !UNITY_WEBGL
		byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(rootNode.ToString());
		Debug.Log(System.Text.Encoding.UTF8.GetString(jsonData));

		if (t4 != null) t4.text += System.Environment.NewLine + System.Text.Encoding.UTF8.GetString(jsonData);

		httpWebRequest.ContentLength = jsonData.Length;
		using (var streamWriter = httpWebRequest.GetRequestStream())
		{
			streamWriter.Write(jsonData, 0, jsonData.Length);
			streamWriter.Flush();
			streamWriter.Close();
		}

		try
		{
			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var result = streamReader.ReadToEnd();
				Debug.Log("POST result" + result);
				if (t4 != null) t4.text += System.Environment.NewLine + "POST result" + System.Environment.NewLine + result;
			}
		}
		catch (WebException ex)
		{

			if (ex.Response != null)
			{
				if (t4 != null) t4.text += ex.Message;
				Debug.Log(ex.Message);
				foreach (DictionaryEntry d in ex.Data)
				{
					Debug.Log(d.ToString());
					if (t4 != null) t4.text += d.ToString();
				}

				string errorDetail = string.Empty;
				using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream(), true))
				{
					errorDetail = streamReader.ReadToEnd();
					Debug.Log(errorDetail);
					if (t4 != null) t4.text += errorDetail;
				}
			}
		}
#endif

		//запускаем отправку completed
		coroutine_completed = PostToLRS_completed();
		StartCoroutine(coroutine_completed);

		yield return new WaitForSeconds(1f);
	}

	//запись в LRS утверждения "completed"
	private IEnumerator PostToLRS_completed()
	{
		if (t4 != null) t4.text += System.Environment.NewLine + "POST completed" + System.Environment.NewLine;

#if !UNITY_WEBGL
		string _endpoint = endpoint + "statements";
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(_endpoint);
		httpWebRequest.ContentType = "application/json; charset=UTF-8";
		httpWebRequest.Method = "POST";
		httpWebRequest.CookieContainer = cookieContainer;
		ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
		string autorization = auth;
		httpWebRequest.Headers.Add("Authorization", autorization);
		httpWebRequest.Headers.Add("X-Experience-API-Version", "1.0.0"); //.1
#endif

		//генерация JSON
		SimpleJSON.JSONNode rootNode = SimpleJSON.JSON.Parse("{}");

		if (_actor_mbox != "") rootNode["actor"]["mbox"] = _actor_mbox;
		if (_actor_account_homePage != "") rootNode["actor"]["account"]["homePage"] = _actor_account_homePage;
		if (_actor_account_name != "") rootNode["actor"]["account"]["name"] = _actor_account_name;

		rootNode["actor"]["name"] = _actor_name; // "Maxim Gammer";
		rootNode["actor"]["objectType"] = "Agent";
		//
		rootNode["verb"]["id"] = "http://adlnet.gov/expapi/verbs/completed"; //passed    //Launched   initialized  completed  experienced answered  passed failed terminated       
		rootNode["verb"]["display"]["en-US"] = "Indicates the actor finished or concluded the activity normally.";
		rootNode["verb"]["display"]["ru-RU"] = "Указывает, что субъект закончил или завершил действие в обычном режиме.";
		//
		rootNode["object"]["id"] = _object_id;//myID;
		rootNode["object"]["objectType"] = "Activity";
		rootNode["object"]["definition"]["name"]["ru-RU"] = myName;
		rootNode["object"]["definition"]["description"]["ru-RU"] = myDiscription;
		//
		rootNode["result"]["completion"] = completion;
		rootNode["result"]["duration"] = "PT" + secounds.ToString() + "S"; //"PT1234S"; //cmi5
		rootNode["result"]["response"] = "Сценарий выполнен";
		rootNode["result"]["extensions"]["https://w3id.org/xapi/cmi5/result/extensions/progress"] = 100;

		rootNode["context"]["registration"] = _context_registration; //Registration; 
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/sessionid"] = _sessionid; // Registration;  //!!!!!!!!!!!!!
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/masteryscore"] = 0.86f;
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/launchurl"] = "https://lcontent.ru";
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/launchmode"] = "Normal";
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/moveon"] = "CompletedOrPassed";
		//
		//for cmi5
			SimpleJSON.JSONArray contextActivities_category = new SimpleJSON.JSONArray();
			//1
			{
				SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
				act1["objectType"] = "Activity";
				act1["id"] = "https://w3id.org/xapi/cmi5/context/categories/cmi5";
				contextActivities_category.Add(act1);
			}
			//2
			{
				SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
				act1["objectType"] = "Activity";
				act1["id"] = "https://w3id.org/xapi/cmi5/context/categories/moveon";
				contextActivities_category.Add(act1);
			}
			//
			rootNode["context"]["contextActivities"]["category"] = contextActivities_category;
			//
			if (_context_contextActivities_grouping_id != "")
			{
				SimpleJSON.JSONArray contextActivities_grouping = new SimpleJSON.JSONArray();
				//1
				{
					SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
					act1["objectType"] = "Activity";
					act1["id"] = _context_contextActivities_grouping_id; //"http://course-repository.example.edu/identifiers/courses/02baafcf/aus/4c07";
					contextActivities_grouping.Add(act1);
				}
				rootNode["context"]["contextActivities"]["grouping"] = contextActivities_grouping;
			}
			if (_context_contextActivities_parent_id != "")
			{
				SimpleJSON.JSONArray contextActivities_parent = new SimpleJSON.JSONArray();
				//1
				{
					SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
					act1["objectType"] = "Activity";
					act1["id"] = _context_contextActivities_parent_id; // "http://course-repository.example.edu/identifiers/courses/02baafcf/aus/4c07";
					contextActivities_parent.Add(act1);
				}
				rootNode["context"]["contextActivities"]["parent"] = contextActivities_parent;
			}
		//for cmi5

		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/weatherConditions"] = "rainy";
		rootNode["context"]["extensions"]["https://w3id.org/xapi/acme/extensions/training-location"] = GetGeo.city;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/geo-location"] = GetGeo.geolocation;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/externalip"] = GetGeo.externalip;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/Device"] = "HTC VIVE";
		rootNode["context"]["platform"] = "lms.lcontent.ru";
		//
		rootNode["context"]["instructor"]["objectType"] = "Agent";
		rootNode["context"]["instructor"]["mbox"] = "mailto:MaxGammer@gmail.com";
		rootNode["context"]["instructor"]["name"] = "Anna Gammer";
		rootNode["context"]["team"]["objectType"] = "Group";
		rootNode["context"]["team"]["mbox"] = "mailto:group@gmail.com";
		rootNode["context"]["team"]["name"] = "Commander ken";
		//

		#if UNITY_WEBGL
			PostJsonToLRS(rootNode.ToString());
		#endif


		#if !UNITY_WEBGL
		byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(rootNode.ToString());
		Debug.Log(System.Text.Encoding.UTF8.GetString(jsonData));

		if (t4 != null) t4.text += System.Environment.NewLine + System.Text.Encoding.UTF8.GetString(jsonData);

		httpWebRequest.ContentLength = jsonData.Length;
		using (var streamWriter = httpWebRequest.GetRequestStream())
		{
			streamWriter.Write(jsonData, 0, jsonData.Length);
			streamWriter.Flush();
			streamWriter.Close();
		}

		try
		{
			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var result = streamReader.ReadToEnd();
				Debug.Log("POST result" + result);
				if (t4 != null) t4.text += System.Environment.NewLine + "POST result" + System.Environment.NewLine + result;
			}
		}
		catch (WebException ex)
		{

			if (ex.Response != null)
			{
				if (t4 != null) t4.text += ex.Message;
				Debug.Log(ex.Message);
				foreach (DictionaryEntry d in ex.Data)
				{
					Debug.Log(d.ToString());
					if (t4 != null) t4.text += d.ToString();
				}

				string errorDetail = string.Empty;
				using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream(), true))
				{
					errorDetail = streamReader.ReadToEnd();
					Debug.Log(errorDetail);
					if (t4 != null) t4.text += errorDetail;
				}
			}
		}
		#endif

		//запускаем отправку completed
		coroutine_passed = PostToLRS_passed();
		StartCoroutine(coroutine_passed);

		yield return new WaitForSeconds(1f);
	}

	//запись в LRS утверждения "passed"
	private IEnumerator PostToLRS_passed()
	{
		if (t4 != null) t4.text += System.Environment.NewLine + "POST passed" + System.Environment.NewLine;

#if !UNITY_WEBGL
		string _endpoint = endpoint + "statements";
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(_endpoint);
		httpWebRequest.ContentType = "application/json; charset=UTF-8";
		httpWebRequest.Method = "POST";
		httpWebRequest.CookieContainer = cookieContainer;
		ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
		string autorization = auth;
		httpWebRequest.Headers.Add("Authorization", autorization);
		httpWebRequest.Headers.Add("X-Experience-API-Version", "1.0.0"); //.1
#endif

		//генерация JSON
		SimpleJSON.JSONNode rootNode = SimpleJSON.JSON.Parse("{}");

		if (_actor_mbox != "") rootNode["actor"]["mbox"] = _actor_mbox;
		if (_actor_account_homePage != "") rootNode["actor"]["account"]["homePage"] = _actor_account_homePage;
		if (_actor_account_name != "") rootNode["actor"]["account"]["name"] = _actor_account_name;

		rootNode["actor"]["name"] = _actor_name; // "Maxim Gammer";
		rootNode["actor"]["objectType"] = "Agent";

		rootNode["verb"]["id"] = "http://adlnet.gov/expapi/verbs/passed"; //passed    //Launched   initialized  completed  experienced answered  passed failed terminated       
		rootNode["verb"]["display"]["en-US"] = "Indicates the actor completed an activity to standard.";
		rootNode["verb"]["display"]["ru-RU"] = "Указывает, что субъект выполнил действие в соответствии со стандартными требованиями.";
		//
		//cmi5 !!! ["object"]["id"] для cmi5 нужно запрашивать из эталона в html (как запрашивается ["context"]["registration"] и /context/extensions/sessionid)
		rootNode["object"]["id"] = _object_id;//myID;
		rootNode["object"]["objectType"] = "Activity";
		rootNode["object"]["definition"]["name"]["ru-RU"] = myName;
		rootNode["object"]["definition"]["description"]["ru-RU"] = myDiscription;
		//
		//rootNode["result"]["completion"] = completion;
		rootNode["result"]["success"] = success;// cmi5
		rootNode["result"]["duration"] = "PT" + secounds.ToString() + "S"; //"PT1234S"; //cmi5
		rootNode["result"]["response"] = "Сценарий выполнен";
		if (max != 0)
		{
			rootNode["result"]["score"]["scaled"] = score / (max - min);
			rootNode["result"]["score"]["raw"] = score;
			rootNode["result"]["score"]["min"] = min;
			rootNode["result"]["score"]["max"] = max;
		}
		else
        {
			rootNode["result"]["score"]["scaled"] = 1;
			rootNode["result"]["score"]["raw"] = 1;
			rootNode["result"]["score"]["min"] = 0;
			rootNode["result"]["score"]["max"] = 1;
		}

		rootNode["result"]["extensions"]["http://lcontent.ru/xapi/Total_losses_money"] = 0;
		rootNode["result"]["extensions"]["http://lcontent.ru/xapi/Total_life_health"] = 0; ;
		rootNode["result"]["extensions"]["http://lcontent.ru/xapi/Total_ecology"] = 0;
		rootNode["result"]["extensions"]["https://w3id.org/xapi/cmi5/result/extensions/progress"] = 100;


		if (examMode == true)
		{
			rootNode["result"]["extensions"]["http://lcontent.ru/xapi/mode"] = "exam";
		}
		else
        {
			rootNode["result"]["extensions"]["http://lcontent.ru/xapi/mode"] = "education";
		}

		//for cmi5
		//https://aicc.github.io/CMI-5_Spec_Current/samples/scenarios/13-progress_usage/

		rootNode["context"]["registration"] = _context_registration; //Registration; 
			rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/sessionid"] = _sessionid; // Registration;  //!!!!!!!!!!!!!
			rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/masteryscore"] = 0.86f;
			rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/launchurl"] = "https://lcontent.ru";
			rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/launchmode"] = "Normal";
			rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/moveon"] = "CompletedOrPassed";
			//
			//for cmi5
			SimpleJSON.JSONArray contextActivities_category = new SimpleJSON.JSONArray();
			//1
			{
				SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
				act1["objectType"] = "Activity";
				act1["id"] = "https://w3id.org/xapi/cmi5/context/categories/cmi5";
				contextActivities_category.Add(act1);
			}
			//2
			{
				SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
				act1["objectType"] = "Activity";
				act1["id"] = "https://w3id.org/xapi/cmi5/context/categories/moveon";
				contextActivities_category.Add(act1);
			}
			//
			rootNode["context"]["contextActivities"]["category"] = contextActivities_category;
			//
			if (_context_contextActivities_grouping_id != "")
			{
				SimpleJSON.JSONArray contextActivities_grouping = new SimpleJSON.JSONArray();
				//1
				{
					SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
					act1["objectType"] = "Activity";
					act1["id"] = _context_contextActivities_grouping_id; //"http://course-repository.example.edu/identifiers/courses/02baafcf/aus/4c07";
					contextActivities_grouping.Add(act1);
				}
				rootNode["context"]["contextActivities"]["grouping"] = contextActivities_grouping;
			}
			if (_context_contextActivities_parent_id != "")
			{
				SimpleJSON.JSONArray contextActivities_parent = new SimpleJSON.JSONArray();
				//1
				{
					SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
					act1["objectType"] = "Activity";
					act1["id"] = _context_contextActivities_parent_id; // "http://course-repository.example.edu/identifiers/courses/02baafcf/aus/4c07";
					contextActivities_parent.Add(act1);
				}
				rootNode["context"]["contextActivities"]["parent"] = contextActivities_parent;
			}
			//for cmi5
		//for cmi5

		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/weatherConditions"] = "rainy";
		rootNode["context"]["extensions"]["https://w3id.org/xapi/acme/extensions/training-location"] = GetGeo.city;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/geo-location"] = GetGeo.geolocation;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/externalip"] = GetGeo.externalip;
		//rootNode["context"]["extensions"]["http://lcontent.ru/xapi/Device"] = "HTC VIVE";

		//
		string video = SystemInfo.graphicsDeviceName;
		string cpu = SystemInfo.processorType;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/DeviceVideo"] = video;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/DeviceCPU"] = cpu;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/currentAvgFPS"] = currentAvgFPS.ToString();

		//
		#if !MM
		var vr = SteamVR.instance;
		if (vr != null)
		{
			rootNode["context"]["extensions"]["http://lcontent.ru/xapi/Device"] = vr.hmd_TrackingSystemName;
			rootNode["context"]["extensions"]["http://lcontent.ru/xapi/DeviceModelNumber"] = vr.hmd_ModelNumber;
			rootNode["context"]["extensions"]["http://lcontent.ru/xapi/DeviceSerialNumber"] = vr.hmd_SerialNumber;
		}
		else
        {
			rootNode["context"]["extensions"]["http://lcontent.ru/xapi/Device"] = "none";
			rootNode["context"]["extensions"]["http://lcontent.ru/xapi/DeviceModelNumber"] = "none";
			rootNode["context"]["extensions"]["http://lcontent.ru/xapi/DeviceSerialNumber"] = "none";
		}
		#endif


		rootNode["context"]["platform"] = "lms.lcontent.ru";
		//
		rootNode["context"]["instructor"]["objectType"] = "Agent";
		rootNode["context"]["instructor"]["mbox"] = "mailto:MaxGammer@gmail.com";
		rootNode["context"]["instructor"]["name"] = "Anna Gammer";
		rootNode["context"]["team"]["objectType"] = "Group";
		rootNode["context"]["team"]["mbox"] = "mailto:group@gmail.com";
		rootNode["context"]["team"]["name"] = "Commander ken";
		//

		{
			//ШАГИ
			SimpleJSON.JSONArray others = new SimpleJSON.JSONArray();
			//string[] steps_ = report.Split(new string[] { System.Environment.NewLine }, System.StringSplitOptions.None);
			for (int i = 0; i < ReportStorage.ReportStorageStepsList.Count; i++)
			{
				SimpleJSON.JSONNode stepNode = SimpleJSON.JSON.Parse("{}");
				stepNode["id"] = "http://lcontent.ru/xapi/step";
				//stepNode["definition"]["type"] = "http://lcontent.ru/xapi/step";
				stepNode["definition"]["name"]["ru-RU"] = ReportStorage.ReportStorageStepsList[i].guid_id;
				stepNode["definition"]["description"]["ru-RU"] = ReportStorage.ReportStorageStepsList[i].definition_description;
				stepNode["definition"]["extensions"]["http://lcontent.ru/step_datatime_real"] = ReportStorage.ReportStorageStepsList[i].datatime_real;//System.DateTime.Now.ToString("yyyy-MM-dd\\ HH:mm:ss\\ ");
				stepNode["definition"]["extensions"]["http://lcontent.ru/step_datatime_simulation"] = ReportStorage.ReportStorageStepsList[i].datatime_simulation;
				stepNode["definition"]["extensions"]["http://lcontent.ru/step_type"] = ReportStorage.ReportStorageStepsList[i].type;
				stepNode["definition"]["extensions"]["http://lcontent.ru/step_completed"] = ReportStorage.ReportStorageStepsList[i].completed;
				stepNode["definition"]["extensions"]["http://lcontent.ru/step_passed"] = ReportStorage.ReportStorageStepsList[i].passed;
				stepNode["definition"]["extensions"]["http://lcontent.ru/step_categoty"] = ReportStorage.ReportStorageStepsList[i].categoty;
				stepNode["objectType"] = "Activity";
				others.Add(stepNode);
			}

			//MM
			{
				SimpleJSON.JSONNode mmNode = SimpleJSON.JSON.Parse("{}");
				mmNode["id"] = "http://lcontent.ru/xapi/mathmodel_value";
				mmNode["definition"]["name"]["ru-RU"] = "Pump1.Q";
				mmNode["definition"]["extensions"]["http://lcontent.ru/mm_dimension"] = "Q, m3/s";
				mmNode["definition"]["extensions"]["http://lcontent.ru/mm_datatime_real"] = System.DateTime.Now.ToString("yyyy-MM-dd\\ HH:mm:ss\\ ");
				mmNode["definition"]["extensions"]["http://lcontent.ru/mm_datatime_simulation"] = System.DateTime.Now.ToString("yyyy-MM-dd\\ HH:mm:ss\\ ");
				mmNode["definition"]["extensions"]["http://lcontent.ru/mm_float_value"] = 56.777f;
				mmNode["definition"]["extensions"]["http://lcontent.ru/mm_str_value"] = "";
				mmNode["objectType"] = "Activity";
				others.Add(mmNode);
			}

			//Последствия
			float summ_losses_money = 0;
			float summ_life_health = 0;
			float summ_ecology = 0;
			//
			for (int i = 0; i < ReportStorage.ReportStorageEffextsList.Count; i++)
			{
				SimpleJSON.JSONNode effectNode = SimpleJSON.JSON.Parse("{}");
				effectNode["id"] = "http://lcontent.ru/xapi/effects";
				effectNode["definition"]["name"]["ru-RU"] = ReportStorage.ReportStorageEffextsList[i].guid_id; 
				effectNode["definition"]["description"]["ru-RU"] = ReportStorage.ReportStorageEffextsList[i].definition_description;
				//ссылка или "", ссылка на id причины для построения диаграмм причинно-следственных связей (ETA/FTA)
				effectNode["definition"]["extensions"]["http://lcontent.ru/effect_ref_parent"] = ReportStorage.ReportStorageEffextsList[i].ref_parent;
				effectNode["definition"]["extensions"]["http://lcontent.ru/effect_datatime_real"] = ReportStorage.ReportStorageEffextsList[i].datatime_real;
				effectNode["definition"]["extensions"]["http://lcontent.ru/effect_datatime_simulation"] = ReportStorage.ReportStorageEffextsList[i].datatime_simulation;
				//причина коротко
				effectNode["definition"]["extensions"]["http://lcontent.ru/effect_cause"] = ReportStorage.ReportStorageEffextsList[i].cause;
				//причина полный
				effectNode["definition"]["extensions"]["http://lcontent.ru/effect_cause_full"] = ReportStorage.ReportStorageEffextsList[i].cause_full;
				//потери общее
				effectNode["definition"]["extensions"]["http://lcontent.ru/effect_losses"] = ReportStorage.ReportStorageEffextsList[i].losses;
				//
				effectNode["definition"]["extensions"]["http://lcontent.ru/effect_losses_moneys"] = ReportStorage.ReportStorageEffextsList[i].losses_money;
				effectNode["definition"]["extensions"]["http://lcontent.ru/effect_losses_life_health"] = ReportStorage.ReportStorageEffextsList[i].losses_life_health;
				effectNode["definition"]["extensions"]["http://lcontent.ru/effect_losses_ecology"] = ReportStorage.ReportStorageEffextsList[i].losses_ecology;
				//
				summ_losses_money += ReportStorage.ReportStorageEffextsList[i].losses_money;
				summ_life_health += ReportStorage.ReportStorageEffextsList[i].losses_life_health;
				summ_ecology += ReportStorage.ReportStorageEffextsList[i].losses_ecology;
				//
				effectNode["objectType"] = "Activity";
				others.Add(effectNode);
				//
			}
			//
			rootNode["result"]["extensions"]["http://lcontent.ru/xapi/Total_losses_money"] = summ_losses_money;
			rootNode["result"]["extensions"]["http://lcontent.ru/xapi/Total_life_health"] = summ_life_health;
			rootNode["result"]["extensions"]["http://lcontent.ru/xapi/Total_ecology"] = summ_ecology;


			//Параметры и неисправности, заданные иснтруктором
			{
				SimpleJSON.JSONNode instructorParameterNode = SimpleJSON.JSON.Parse("{}");
				instructorParameterNode["id"] = "http://lcontent.ru/xapi/instructor_parameter";
				instructorParameterNode["definition"]["name"]["ru-RU"] = "1";
				instructorParameterNode["definition"]["extensions"]["http://lcontent.ru/instructor_parameter_name"] = "Pump1.Z";
				instructorParameterNode["definition"]["extensions"]["http://lcontent.ru/instructor_parameter_dimension"] = "количество ступеней, шт.";
				instructorParameterNode["definition"]["extensions"]["http://lcontent.ru/instructor_parameter_datatime_real"] = System.DateTime.Now.ToString("yyyy-MM-dd\\ HH:mm:ss\\ ");
				instructorParameterNode["definition"]["extensions"]["http://lcontent.ru/instructor_parameter_datatime_simulation"] = System.DateTime.Now.ToString("yyyy-MM-dd\\ HH:mm:ss\\ ");
				instructorParameterNode["definition"]["extensions"]["http://lcontent.ru/instructor_parameter_float_value"] = 56.777f;
				instructorParameterNode["definition"]["extensions"]["http://lcontent.ru/instructor_parameter_str_value"] = "";
				instructorParameterNode["objectType"] = "Activity";
				others.Add(instructorParameterNode);
			}
			{
				SimpleJSON.JSONNode instructorProblemNode = SimpleJSON.JSON.Parse("{}");
				instructorProblemNode["id"] = "http://lcontent.ru/xapi/instructor_problem";
				instructorProblemNode["definition"]["name"]["ru-RU"] = "1";
				instructorProblemNode["definition"]["extensions"]["http://lcontent.ru/instructor_problem_name"] = "Задвижка3.Клин";
				instructorProblemNode["definition"]["extensions"]["http://lcontent.ru/instructor_problem_dimension"] = "да или нет";
				instructorProblemNode["definition"]["extensions"]["http://lcontent.ru/instructor_problem_datatime_real"] = System.DateTime.Now.ToString("yyyy-MM-dd\\ HH:mm:ss\\ ");
				instructorProblemNode["definition"]["extensions"]["http://lcontent.ru/instructor_problem_datatime_simulation"] = System.DateTime.Now.ToString("yyyy-MM-dd\\ HH:mm:ss\\ ");
				instructorProblemNode["definition"]["extensions"]["http://lcontent.ru/instructor_problem_float_value"] = 0;
				instructorProblemNode["definition"]["extensions"]["http://lcontent.ru/instructor_problem_str_value"] = "да";
				instructorProblemNode["objectType"] = "Activity";
				others.Add(instructorProblemNode);
			}
			{
				SimpleJSON.JSONNode instructorCommentNode = SimpleJSON.JSON.Parse("{}");
				instructorCommentNode["id"] = "http://lcontent.ru/xapi/instructor_comment";
				instructorCommentNode["definition"]["name"]["ru-RU"] = "1";
				instructorCommentNode["definition"]["extensions"]["http://lcontent.ru/instructor_comment_text"] = "Грубое нарушение техники безопасности.н";
				instructorCommentNode["definition"]["extensions"]["http://lcontent.ru/instructor_comment_datatime_real"] = System.DateTime.Now.ToString("yyyy-MM-dd\\ HH:mm:ss\\ ");
				instructorCommentNode["definition"]["extensions"]["http://lcontent.ru/instructor_comment_datatime_simulation"] = System.DateTime.Now.ToString("yyyy-MM-dd\\ HH:mm:ss\\ ");
				instructorCommentNode["objectType"] = "Activity";
				others.Add(instructorCommentNode);
			}


			//Лог нейроинтерфейса
			{
				SimpleJSON.JSONArray _data = new SimpleJSON.JSONArray();
				SimpleJSON.JSONNode _A = SimpleJSON.JSON.Parse("{}");
				_A["value"] = 10.0f;
				SimpleJSON.JSONNode _B = SimpleJSON.JSON.Parse("{}");
				_B["value"] = 11.0f;
				_data.Add(_A);
				_data.Add(_B);


				SimpleJSON.JSONNode neurointerfaceNode = SimpleJSON.JSON.Parse("{}");
				neurointerfaceNode["id"] = "http://lcontent.ru/xapi/neurointerface_log";
				neurointerfaceNode["definition"]["description"]["ru-RU"] = "Лог нейроинтерфейса";
				neurointerfaceNode["definition"]["name"]["ru-RU"] = "1";
				neurointerfaceNode["definition"]["extensions"]["http://lcontent.ru/neurointerface_log_model"] = "OpenBCD ";
				neurointerfaceNode["definition"]["extensions"]["http://lcontent.ru/neurointerface_log_datatime_real"] = System.DateTime.Now.ToString("yyyy-MM-dd\\ HH:mm:ss\\ ");
				neurointerfaceNode["definition"]["extensions"]["http://lcontent.ru/neurointerface_log_datatime_simulation"] = System.DateTime.Now.ToString("yyyy-MM-dd\\ HH:mm:ss\\ ");
				neurointerfaceNode["definition"]["extensions"]["http://lcontent.ru/neurointerface_log_data"] = _data;
				neurointerfaceNode["objectType"] = "Activity";
				others.Add(neurointerfaceNode);
			}

			//
			rootNode["context"]["contextActivities"]["other"] = others;
		}

		//Конец генерация JSON

#if UNITY_WEBGL
			PostJsonToLRS(rootNode.ToString());
#endif

#if !UNITY_WEBGL
		byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(rootNode.ToString());
		Debug.Log(System.Text.Encoding.UTF8.GetString(jsonData));

		if (t4 != null) t4.text += System.Environment.NewLine + System.Text.Encoding.UTF8.GetString(jsonData);

		httpWebRequest.ContentLength = jsonData.Length;
		using (var streamWriter = httpWebRequest.GetRequestStream())
		{
			streamWriter.Write(jsonData,0,jsonData.Length);
			streamWriter.Flush();
			streamWriter.Close();
		}

		try
		{
			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var result = streamReader.ReadToEnd();
				Debug.Log("POST result"+result);
				if (t4 != null) t4.text += System.Environment.NewLine + "POST result" + System.Environment.NewLine + result;
			}
		}
		catch(WebException ex)
		{
			if (ex.Response != null)
			{
				if (t4 != null) t4.text += ex.Message;
				Debug.Log(ex.Message);
				foreach (DictionaryEntry d in ex.Data)
				{
					Debug.Log(d.ToString());
					if (t4 != null) t4.text += d.ToString();
				}

				string errorDetail = string.Empty;
				using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream(), true))
				{
					errorDetail = streamReader.ReadToEnd();
					Debug.Log(errorDetail);
					if (t4 != null) t4.text += errorDetail;
				}
			}
		}
#endif

		yield return new WaitForSeconds(1f);

		//запускаем отправку terminated
		coroutine_terminated = PostToLRS_terminated();
		StartCoroutine(coroutine_terminated);
	}

	//запись в LRS утверждения "terminated"
	private IEnumerator PostToLRS_terminated()
	{
		if (t4 != null) t4.text += System.Environment.NewLine + "POST terminated" + System.Environment.NewLine;

#if !UNITY_WEBGL
		string _endpoint = endpoint + "statements";

		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(_endpoint);
		httpWebRequest.ContentType = "application/json; charset=UTF-8";
		httpWebRequest.Method = "POST";
		httpWebRequest.CookieContainer = cookieContainer;
		ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

		string autorization = auth;
		httpWebRequest.Headers.Add("Authorization", autorization);
		httpWebRequest.Headers.Add("X-Experience-API-Version", "1.0.0"); //.1
#endif

		//генерация JSON
		SimpleJSON.JSONNode rootNode = SimpleJSON.JSON.Parse("{}");
		//
		if (_actor_mbox != "") rootNode["actor"]["mbox"] = _actor_mbox;
		if (_actor_account_homePage != "") rootNode["actor"]["account"]["homePage"] = _actor_account_homePage;
		if (_actor_account_name != "") rootNode["actor"]["account"]["name"] = _actor_account_name;

		rootNode["actor"]["name"] = _actor_name; // "Maxim Gammer";
		rootNode["actor"]["objectType"] = "Agent";
		//
		rootNode["verb"]["id"] = "http://adlnet.gov/expapi/verbs/terminated"; //passed    //Launched   initialized  completed  experienced answered  passed failed terminated       
		rootNode["verb"]["display"]["en-US"] = "Indicates the actor has completed their session normally.";
		rootNode["verb"]["display"]["ru-RU"] = "Указывает, что субъект нормально завершил сеанс.";

		rootNode["object"]["id"] = _object_id;//myID;
		rootNode["object"]["objectType"] = "Activity";
		rootNode["object"]["definition"]["name"]["ru-RU"] = myName;
		rootNode["object"]["definition"]["description"]["ru-RU"] = myDiscription;
		
		rootNode["result"]["duration"] = "PT" + secounds.ToString() + "S"; //"PT1234S"; //cmi5
		rootNode["result"]["response"] = "Сценарий выполнен";
		rootNode["result"]["extensions"]["https://w3id.org/xapi/cmi5/result/extensions/progress"] = 100;
		rootNode["result"]["extensions"]["http://lcontent.ru/xapi/Total_losses_money"] = 0;
		rootNode["result"]["extensions"]["http://lcontent.ru/xapi/Total_life_health"] = 0 ;
		rootNode["result"]["extensions"]["http://lcontent.ru/xapi/Total_ecology"] = 0;

		//
		rootNode["context"]["registration"] = _context_registration; //Registration; 
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/sessionid"] = _sessionid; // Registration;  //!!!!!!!!!!!!!
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/masteryscore"] = 0.86f;
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/launchurl"] = "https://lcontent.ru";
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/launchmode"] = "Normal";
		rootNode["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/moveon"] = "CompletedOrPassed";
		//
		//for cmi5
			SimpleJSON.JSONArray contextActivities_category = new SimpleJSON.JSONArray();
			//1
			{
				SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
				act1["objectType"] = "Activity";
				act1["id"] = "https://w3id.org/xapi/cmi5/context/categories/cmi5";
				contextActivities_category.Add(act1);
			}
			//
			rootNode["context"]["contextActivities"]["category"] = contextActivities_category;
			//
			if (_context_contextActivities_grouping_id != "")
			{
				SimpleJSON.JSONArray contextActivities_grouping = new SimpleJSON.JSONArray();
				//1
				{
					SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
					act1["objectType"] = "Activity";
					act1["id"] = _context_contextActivities_grouping_id; //"http://course-repository.example.edu/identifiers/courses/02baafcf/aus/4c07";
					contextActivities_grouping.Add(act1);
				}
				rootNode["context"]["contextActivities"]["grouping"] = contextActivities_grouping;
			}
			if (_context_contextActivities_parent_id != "")
			{
				SimpleJSON.JSONArray contextActivities_parent = new SimpleJSON.JSONArray();
				//1
				{
					SimpleJSON.JSONNode act1 = SimpleJSON.JSON.Parse("{}");
					act1["objectType"] = "Activity";
					act1["id"] = _context_contextActivities_parent_id; // "http://course-repository.example.edu/identifiers/courses/02baafcf/aus/4c07";
					contextActivities_parent.Add(act1);
				}
				rootNode["context"]["contextActivities"]["parent"] = contextActivities_parent;
			}
		//for cmi5

		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/weatherConditions"] = "rainy";
		rootNode["context"]["extensions"]["https://w3id.org/xapi/acme/extensions/training-location"] = GetGeo.city;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/geo-location"] = GetGeo.geolocation;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/externalip"] = GetGeo.externalip;
		rootNode["context"]["extensions"]["http://lcontent.ru/xapi/Device"] = "HTC VIVE";
		rootNode["context"]["platform"] = "lms.lcontent.ru";
		//
		rootNode["context"]["instructor"]["objectType"] = "Agent";
		rootNode["context"]["instructor"]["mbox"] = "mailto:MaxGammer@gmail.com";
		rootNode["context"]["instructor"]["name"] = "Anna Gammer";
		rootNode["context"]["team"]["objectType"] = "Group";
		rootNode["context"]["team"]["mbox"] = "mailto:group@gmail.com";
		rootNode["context"]["team"]["name"] = "Commander ken";
		//Конец генерация JSON

#if UNITY_WEBGL
			PostJsonToLRS(rootNode.ToString());
#endif

#if !UNITY_WEBGL
		byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(rootNode.ToString());
		Debug.Log(System.Text.Encoding.UTF8.GetString(jsonData));

		if (t4 != null) t4.text += System.Environment.NewLine + System.Text.Encoding.UTF8.GetString(jsonData);

		httpWebRequest.ContentLength = jsonData.Length;
		using (var streamWriter = httpWebRequest.GetRequestStream())
		{
			streamWriter.Write(jsonData, 0, jsonData.Length);
			streamWriter.Flush();
			streamWriter.Close();
		}

		try
		{
			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var result = streamReader.ReadToEnd();
				Debug.Log("POST result" + result);
				if (t4 != null) t4.text += System.Environment.NewLine + "POST result" + System.Environment.NewLine + result;
			}
		}
		catch (WebException ex)
		{

			if (ex.Response != null)
			{
				if (t4 != null) t4.text += ex.Message;
				Debug.Log(ex.Message);
				foreach (DictionaryEntry d in ex.Data)
				{
					Debug.Log(d.ToString());
					if (t4 != null) t4.text += d.ToString();
				}

				string errorDetail = string.Empty;
				using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream(), true))
				{
					errorDetail = streamReader.ReadToEnd();
					Debug.Log(errorDetail);
					if (t4 != null) t4.text += errorDetail;
				}
			}
		}
#endif

		yield return new WaitForSeconds(1f);
	}

	//получение данных из LRS утверждения "launched" (формируется до запуска тренажера) если exe! , если WebGL то не вызывается, вместо нее JS вызывает GetDataFronJSON
	public void GetData()
	{
		//запускаем процедуру проверки
		coroutine_get = GetFromLRS();
		StartCoroutine(coroutine_get);

		if (Application.isEditor == true)
		{
			if (ShieldOn == true)
			{
				//запускаем процедуру проверки
				coroutine_check = CheckLRS();
				StartCoroutine(coroutine_check);
			}
		}
	}

	//функция вызывается или из GetFromLRS если exe или из JS если WebGL
	public void GetDataFronJSON(string result)
    {
		Debug.Log(System.Environment.NewLine + "GET result:" + result);
		if (t4 != null) t4.text += System.Environment.NewLine + "GET result" + System.Environment.NewLine + result;

		SimpleJSON.JSONNode rootNode = SimpleJSON.JSON.Parse(result);

		_actor_mbox = "";
		_actor_account_homePage = "";
		_actor_account_name = "";
		if (rootNode["statements"][0]["actor"]["mbox"].IsString == true) //Count //
		{
			_actor_mbox = rootNode["statements"][0]["actor"]["mbox"];
		}
		if (rootNode["statements"][0]["actor"]["account"]["homePage"].IsString == true)
		{
			_actor_account_homePage = rootNode["statements"][0]["actor"]["account"]["homePage"];

		}
		if (rootNode["statements"][0]["actor"]["account"]["name"].IsString == true)
		{
			_actor_account_name = rootNode["statements"][0]["actor"]["account"]["name"];
		}

		if ((_actor_mbox == "") & (_actor_account_homePage == "") && (_actor_account_name == ""))
		{
			_actor_mbox = "mailto:user@lcontent.ru";
		}

		if (rootNode["statements"][0]["actor"]["name"].IsString == true)
		{
			_actor_name = rootNode["statements"][0]["actor"]["name"];
		}
		else
		{
			_actor_name = "noname";
		}

		if (rootNode["statements"][0]["object"]["id"].IsString == true)
		{
			_object_id = rootNode["statements"][0]["object"]["id"];
		}
		else
		{
			_object_id = "simulation://xapitest";
		}

		if (rootNode["statements"][0]["context"]["registration"].IsString == true)
		{
			_context_registration = rootNode["statements"][0]["context"]["registration"];
			if (Registration=="")
            {
				Registration = _context_registration;
			}
		}
		else
		{
			_context_registration = System.Guid.NewGuid().ToString();
			if (Registration == "")
			{
				Registration = _context_registration;
			}
		}

		if (rootNode["statements"][0]["object"]["definition"]["name"]["en-US"].IsString == true)
		{
			myName = rootNode["statements"][0]["object"]["definition"]["name"]["en-US"];
		}
		if (rootNode["statements"][0]["object"]["definition"]["description"]["en-US"].IsString == true)
		{
			myDiscription = rootNode["statements"][0]["object"]["definition"]["description"]["en-US"];
		}

		_sessionid = _context_registration;
		if (rootNode["statements"][0]["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/sessionid"].IsString == true) //Count //
		{
			_sessionid = rootNode["statements"][0]["context"]["extensions"]["https://w3id.org/xapi/cmi5/context/extensions/sessionid"];
		}

		_context_contextActivities_grouping_id = "";
		_context_contextActivities_parent_id = "";

		if (rootNode["statements"][0]["context"]["contextActivities"]["grouping"][0]["id"].IsString == true)
		{
			_context_contextActivities_grouping_id = rootNode["statements"][0]["context"]["contextActivities"]["grouping"][0]["id"];
		}
		if (rootNode["statements"][0]["context"]["contextActivities"]["parent"][0]["id"].IsString == true)
		{
			_context_contextActivities_parent_id = rootNode["statements"][0]["context"]["contextActivities"]["parent"][0]["id"];
		}


		if (t4 != null)
		{
			t4.text += System.Environment.NewLine + "email=" + _actor_mbox;
			t4.text += System.Environment.NewLine + "name=" + _actor_name;
			t4.text += System.Environment.NewLine + "object.id=" + _object_id;
			t4.text += System.Environment.NewLine + "context.registration=" + _context_registration;
			t4.text += System.Environment.NewLine + "context.contextActivities.parent=" + _context_contextActivities_parent_id;
		}
		else
		{
			Debug.Log("email=" + _actor_mbox);
			Debug.Log("name=" + _actor_name);
			Debug.Log("object.id=" + _object_id);
			Debug.Log("context.registration=" + _context_registration);
			Debug.Log("context.contextActivities.parent=" + _context_contextActivities_parent_id);
		}

		if (FIO_Text != null)
		{
			FIO_Text.text = _actor_name;
		}

		if (ShieldOn == true)
		{
			//перходим к проверке защиты
			ShieldCheck();
		}
		
	}

	//получение данных из LRS утверждения "launched" (формируется до запуска тренажера)
	private IEnumerator GetFromLRS()
	{
		if (t4 != null) t4.text += System.Environment.NewLine + "GET" + System.Environment.NewLine;

		string _endpoint = endpoint + "statements";
		string autorization = auth;
		string _registration = Registration;

		string UriString = _endpoint + "?registration=" + _registration + "&" + "verb=" + "http://adlnet.gov/expapi/verbs/launched";
		Debug.Log(UriString);
		if (t4 != null) t4.text += System.Environment.NewLine + UriString;

		cookieContainer = new CookieContainer();
		
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(UriString);
		httpWebRequest.ContentType = "application/json; charset=UTF-8";
		httpWebRequest.Method = "GET";
		httpWebRequest.CookieContainer = cookieContainer;
		ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
		//
		httpWebRequest.Headers.Add("Authorization", autorization);
		//
		httpWebRequest.Headers.Add("X-Experience-API-Version", "1.0.0"); //.1

		try
		{
			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var result = streamReader.ReadToEnd();
				GetDataFronJSON(result);
			}
		}
		catch (WebException ex)
		{

			if (ex.Response != null)
			{
				if (t4 != null) t4.text += ex.Message;
				Debug.Log(ex.Message);
				foreach (DictionaryEntry d in ex.Data)
				{
					Debug.Log(d.ToString());
					if (t4 != null) t4.text += d.ToString();
				}

				string errorDetail = string.Empty;
				using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream(), true))
				{
					errorDetail = streamReader.ReadToEnd();
					Debug.Log(errorDetail);
					if (t4 != null) t4.text += errorDetail;
				}
			}
		}
		yield return new WaitForSeconds(1f);
	}

	//защита
	void ShieldCheck()
    {
		if (Application.isEditor == true) return;

		if (_context_contextActivities_parent_id == "")
		{
			ShowErrorNull();
			return;
		}
		

		System.Uri myUri = new System.Uri(_context_contextActivities_parent_id);
		Lms_Host = myUri.Scheme + System.Uri.SchemeDelimiter + myUri.Host;  

		if (t4 != null)
		{
			t4.text += System.Environment.NewLine + "host=" + Lms_Host;
		}

		//Для WebGL вызывается Check из JS
		#if !UNITY_WEBGL
			//запускаем процедуру проверки
			coroutine_check = CheckLRS();
			StartCoroutine(coroutine_check);
		#endif
	}

	private void ShowErrorNull()
    {
		if (t4 != null) t4.text += System.Environment.NewLine + "Нет записи о лицензии" + System.Environment.NewLine;
		if ((alertText != null) && (CanvasShield != null))
		{
			alertText.text += "Нет записи о лицензии" + System.Environment.NewLine;
			CanvasShield.SetActive(true);
		}
	}

	//процедура проверки - анализ полученных данных, запускается напрямую из CheckLRS, если мы exe и запускается из JS если мы WebGL
	public void Check(string result)
    {
		Debug.Log(System.Environment.NewLine + "GET result:" + result);
		if (t4 != null) t4.text += System.Environment.NewLine + "GET result" + System.Environment.NewLine + result;
		{
			SimpleJSON.JSONNode rootNode = SimpleJSON.JSON.Parse(result);
			//if (rootNode["statements"][0]["actor"]["mbox"].IsString == true) //Count //
			if (rootNode["statements"].Count > 0)
			{
				string homePage = rootNode["statements"][0]["actor"]["account"]["homePage"];
				string orgName = rootNode["statements"][0]["actor"]["account"]["name"];

				string LicenseExpirationDate = rootNode["statements"][0]["object"]["definition"]["extensions"]["https://lcontent.ru/xapi/LicenseExpirationDate"];
				string LicenseActive = rootNode["statements"][0]["object"]["definition"]["extensions"]["https://lcontent.ru/xapi/LicenseActive"];
				string LicenseDemoMode = rootNode["statements"][0]["object"]["definition"]["extensions"]["https://lcontent.ru/xapi/LicenseDemoMode"];

				if (t4 != null)
				{
					t4.text += System.Environment.NewLine;
					t4.text += "homePage=" + homePage + System.Environment.NewLine;
					t4.text += "orgName=" + orgName + System.Environment.NewLine;
					t4.text += "LicenseExpirationDate=" + LicenseExpirationDate + System.Environment.NewLine;
					t4.text += "LicenseActive=" + LicenseActive + System.Environment.NewLine;
					t4.text += "LicenseDemoMode=" + LicenseDemoMode + System.Environment.NewLine;
				}

				if ((NameText != null) && (LicenseText != null))
				{
					NameText.text = myName;
					LicenseText.text = "Правом использования обладает: " + orgName + System.Environment.NewLine + "LMS: " + homePage + System.Environment.NewLine + "Дата лицензии: " + LicenseExpirationDate + System.Environment.NewLine + "Лицензия активна: " + LicenseActive + System.Environment.NewLine;
				}

				if ((alertText != null) && (CanvasShield != null))
				{
					alertText.text += "endpoint=" + endpoint + System.Environment.NewLine;
					alertText.text += "Lms_Host=" + Lms_Host + System.Environment.NewLine;
					alertText.text += "Organization=" + HostOrOrganization + System.Environment.NewLine;
					alertText.text += "object=" + _object_id + System.Environment.NewLine;

					alertText.text += System.Environment.NewLine;
				}

				System.DateTime d1 = System.DateTime.Parse(LicenseExpirationDate, null, System.Globalization.DateTimeStyles.RoundtripKind);
				System.DateTime d2 = System.DateTime.Now;
				if (d1 < d2)
				{
					int temp = (int)(d2 - d1).TotalDays;
					if (t4 != null)
					{
						t4.text += "время лицензии истекло" + System.Environment.NewLine;
						t4.text += "дней просрочено=" + temp.ToString() + System.Environment.NewLine;
					}

					if ((alertText != null) && (CanvasShield != null))
					{
						alertText.text += "Время лицензии истекло (дней просрочено=" + temp.ToString() + ")" + System.Environment.NewLine;
						CanvasShield.SetActive(true);
					}

				}
				else
				{
					if (t4 != null) t4.text += "время лицензии .. ок" + System.Environment.NewLine;
				}

				if (LicenseActive == "true")
				{
					if (t4 != null) t4.text += "лицензия разрешена" + System.Environment.NewLine;
				}
				else
				{
					if (t4 != null) t4.text += "Лицензия запрещена" + System.Environment.NewLine;

					if ((alertText != null) && (CanvasShield != null))
					{
						if (LicenseDemoMode == "false")
						{
							alertText.text += "Лицензия запрещена" + System.Environment.NewLine;
							CanvasShield.SetActive(true);
						}
					}
				}

				if ((LicenseDemoMode == "true") && (LicenseActive == "false"))
				{
					if (t4 != null) t4.text += "Демо-режим разрешен" + System.Environment.NewLine;

					if (CanvasDemo != null)
					{
						CanvasDemo.SetActive(true);
						//CanvasDemoTimeOverPanel
					}
				}
			}
			else
			{
				if (t4 != null) t4.text += System.Environment.NewLine + "Нет записи о лицензии" + System.Environment.NewLine;
				if ((alertText != null) && (CanvasShield != null))
				{
					alertText.text += "Нет записи о лицензии" + System.Environment.NewLine;
					CanvasShield.SetActive(true);
				}
			}


			if ((alertText != null) && (CanvasShield != null))
			{
				if (CanvasShield.activeSelf == true)
				{
					alertText.text += System.Environment.NewLine + support_text;
				}
			}

		}
	}

	//процедура проверки
	private IEnumerator CheckLRS()
	{
		//делаем запрос по
		//_object_id  simulation://xapitest
		//verb = https://lcontent.ru/xapi/verbs/licensed
		//account.homePage = lms (с добавление https:// и http://)  +- можно еще account.organization если известна последняя

		if (t4 != null) t4.text += System.Environment.NewLine + "CHECK" + System.Environment.NewLine;

		string _endpoint = endpoint + "statements";
		string autorization = auth;

		SimpleJSON.JSONNode agent1 = SimpleJSON.JSON.Parse("{}");
		agent1["account"]["homePage"] = Lms_Host;
		//agent1["objectType"] = "Agent";
		agent1["account"]["name"] = HostOrOrganization;
		
		string agent = agent1.ToString(); 

		string UriString = _endpoint + "?verb=" + "https://lcontent.ru/xapi/verbs/licensed" + "&" + "activity=" + _object_id + "&" + "agent=" + agent + "&" + "limit=1"; 
		if (t4 != null) t4.text += System.Environment.NewLine + UriString;
		cookieContainer = new CookieContainer();
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(UriString);
		httpWebRequest.ContentType = "application/json; charset=UTF-8";
		httpWebRequest.Method = "GET";
		httpWebRequest.CookieContainer = cookieContainer;
		ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
		httpWebRequest.Headers.Add("Authorization", autorization);
		httpWebRequest.Headers.Add("X-Experience-API-Version", "1.0.0"); //.1
		try
		{
			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var result = streamReader.ReadToEnd();
				Check(result);
			}
		}
		catch (WebException ex)
		{

			if (ex.Response != null)
			{
				if ((alertText != null) && (CanvasShield != null))
				{
					alertText.text += ex.Message + System.Environment.NewLine;
					CanvasShield.SetActive(true);
				}

				if (t4 != null) t4.text += ex.Message;
				Debug.Log(ex.Message);

				foreach (DictionaryEntry d in ex.Data)
				{
					Debug.Log(d.ToString());
					if (t4 != null) t4.text += d.ToString();
				}

				string errorDetail = string.Empty;
				using (StreamReader streamReader = new StreamReader(ex.Response.GetResponseStream(), true))
				{
					errorDetail = streamReader.ReadToEnd();
					Debug.Log(errorDetail);
					if (t4 != null) t4.text += errorDetail;
				}
			}
		}
		yield return new WaitForSeconds(1f);
	}

}

/*
	{
		actor: 
		{
			objectType: "Agent",
			account: 
			{
			homePage: "https://lms.lcontent.ru",
			name: "ООО ИНК"
			}
		},
		verb: 
		{
			id: "https://lcontent.ru/xapi/verbs/licensed",
			display: 
			{
				en-US: "licensed"
			}
		},
		object: 
		{
			id: "simulation://xapitest",
			definition: 
			{
				type: "https://lcontent.ru/xapi/simulator",
				name: 
				{
					en-US: "Первая помощь"
				},
				description: 
				{
					en-US: "Первая помощь. 10 сценариев."
				},
				extensions: 
				{
					"https://lcontent.ru/xapi/LicenseExpirationDate": "2021-10-05",
					"https://lcontent.ru/xapi/LicenseActive": "true",
					"https://lcontent.ru/xapi/LicenseDemoMode": "true"
				}
			}
		}
	}
*/


