using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft;


using UnityEngine;
using UnityEngine.Networking;


public class API : MonoBehaviour {
	private const string AUTHURL = "https://api.hexoskin.com/api/connect/oauth2/token/";
	private const string URL = "https://api.hexoskin.com/api/user";

	private string AUTHTOKEN;
	private string REFRESHTOKEN;

	/* Container class for auth token
     * 
     */
	public class AuthResponse
	{
		public string access_token { get; set; }
		public string token_type { get; set; }
		public int expires_in { get; set; }
		public string refresh_token { get; set; }
		public string scope { get; set; }
	}

    //Main function
    public void Request(){
		WWWForm form = new WWWForm ();
		print("Requesting");
        StartCoroutine(RequestToken());

        //Parse Auth token

		//By this point valid auth token has been given
		//Dictionary<string, string> headers = form.headers;
		//headers ["Authorization"] = "Bearer " + AUTHTOKEN;

		//WWW request = new WWW(URL, form);
		//StartCoroutine (OnResponse (request));

		//if (request.error == "401 Unauthorized") {
		//Token expired, refresh
		//Debug.Log (request.error);
		//requestToken (form, true);
		//}
	}

    public void parseAuth(string jsonResponse){
        AuthResponse tokens = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthResponse>(jsonResponse);
		AUTHTOKEN = tokens.access_token;
		REFRESHTOKEN = tokens.refresh_token;
		print ("AUTH " + AUTHTOKEN);
		print ("REFRESH " + REFRESHTOKEN);
    }

	/* Requests new access token
	 * 
	 */
	private IEnumerator RequestToken(){
		//Create and send auth request to Hexoskin
		print("Creating auth request");
		WWWForm refreshForm = new WWWForm ();
		refreshForm.AddField("username", "webert3@wwu.edu");
		refreshForm.AddField("password", "neat_lab_2018");
		refreshForm.AddField("scope", "readonly");
		refreshForm.AddField("grant_type", "password");

		UnityWebRequest refreshReq = UnityWebRequest.Post(AUTHURL, refreshForm);
		refreshReq.SetRequestHeader("Authorization", "Basic Y0JjaXdYY0pYZlljNURsNWNRMnJid0Z1bVljbVFMOjdkNjE4dE1ydnNRSDJOVzhoWlBybDJyNk9USEttaw==");
		refreshReq.chunkedTransfer = false;

		print("Submitting");
		yield return refreshReq.SendWebRequest();
		print("Response received");

		//Check for errors
		if (refreshReq.error == null){
			print("Auth Success");
			print(refreshReq.downloadHandler.text);
			//Parse response for auth
			parseAuth(refreshReq.downloadHandler.text);
		}
		else{
			print("Refresh error " + refreshReq.error);
		}
	}


	//Handles general response
	private IEnumerator OnResponse(WWW req){
		yield return req;
		// check for errors
		if (req.error == null) {
			print("Auth Success");
			print(req.text);
		} else {
			Debug.Log (req.error);
		}
	}
}