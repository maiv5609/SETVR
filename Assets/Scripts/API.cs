using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;


public class API : MonoBehaviour {
	private const string AUTHURL = "https://api.hexoskin.com/api/connect/oauth2/token/";
	private const string URL = "https://api.hexoskin.com/api/user";

    private const string username = "webert3@wwu.edu";
    private const string password = "neat_lab_2018";
    private const string scope = "readonly";
    private const string grant_type = "password";

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

        public AuthResponse(string access_token, string token_type, int expires_in, string refresh_token, string scope){
            this.access_token = access_token;
            this.token_type = token_type;
            this.expires_in = expires_in;
            this.refresh_token = refresh_token;
            this.scope = scope;
        }

        public static AuthResponse CreateFromJSON<T>(string jsonString)
        {
            return JsonUtility.FromJson<AuthResponse>(jsonString);
        }

        //JsonUtility doesn't support parsing top level json array so you need to wrap it in an object
        [Serializable]
        private class Wrapper<String>{
            public String[] Items;
        }
    }

    //Main function
    public void Request(){
		WWWForm form = new WWWForm ();
        print("Requesting");
        StartCoroutine(RequestToken());

        //By this point valid auth token has been given
		Dictionary<string, string> headers = form.headers;
		headers ["Authorization"] = "Bearer " + AUTHTOKEN;

		//WWW request = new WWW(URL, form);
		//StartCoroutine (OnResponse (request));

		//if (request.error == "401 Unauthorized") {
			//Token expired, refresh
			//Debug.Log (request.error);
			//requestToken (form, true);
		//}
	}

	/* Requests new access token
	 * 
	 */
	private IEnumerator RequestToken(){
        print("Creating auth request");
		WWWForm refreshForm = new WWWForm ();
        refreshForm.AddField("username", "webert3@wwu.edu");
        refreshForm.AddField("password", "neat_lab_2018");
        refreshForm.AddField("scope", "readonly");
        refreshForm.AddField("grant_type", "password");

        //refreshForm.headers["Authorization"] = "Basic Y0JjaXdYY0pYZlljNURsNWNRMnJid0Z1bVljbVFMOjdkNjE4dE1ydnNRSDJOVzhoWlBybDJyNk9USEttaw==";
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
            AuthResponse tokens = AuthResponse.CreateFromJSON(refreshReq.downloadHandler.text);
            AUTHTOKEN = tokens.access_token;
            REFRESHTOKEN = tokens.refresh_token;
            print("AUTH: " + tokens.access_token);
            print("REFRESH: " + REFRESHTOKEN);
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
