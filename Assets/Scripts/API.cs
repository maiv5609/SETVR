using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft;


using UnityEngine;
using UnityEngine.Networking;

//TODO: Work on realtime request and switching starting timestamp to last available timestamp from previous request

public class API : MonoBehaviour {
    public int USERID;
	private const string AUTHURL = "https://api.hexoskin.com/api/connect/oauth2/token/";
	private const string URL = "https://api.hexoskin.com/api/user";

	private string AUTHTOKEN;
	private string REFRESHTOKEN;

    //Base times for requests
    private int startTime;
    private int currentTime;
    private int endTime;

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

        //Set current timestamp for realtime request, need to multiply this by 256 before request
        TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
        int secondsSinceEpoch = (int)t.TotalSeconds;
        startTime = secondsSinceEpoch;
        currentTime = secondsSinceEpoch;

        endTime = startTime + 86400; //End time 24 hours from then

        //By this point valid auth token has been given Repeat call every 15 seconds
        print("Requesting Realtime");
        StartCoroutine(RealTimeRequest());
    }

    /* Parsing Functions */

    public void ParseAuth(string jsonResponse){
        AuthResponse tokens = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthResponse>(jsonResponse);
		AUTHTOKEN = tokens.access_token;
		REFRESHTOKEN = tokens.refresh_token;
		print ("AUTH " + AUTHTOKEN);
		print ("REFRESH " + REFRESHTOKEN);
    }

    /* Requesting Functions */

    /* Makes a request for realtime data
     * Called with the assumption that there is a valid auth token
     * Requests should ask for 15 seconds of data every 15 seconds
     * https://api.hexoskin.com/api/data/?user=14052&datatype=18&start=392531420416&end=392541193472&flat=1&no_timestamps=exact
     */
    private IEnumerator RealTimeRequest(){
        //Have to use while true with a sleep in order to continuing calling every 15 seconds
        while (true){
            //Create and send auth request to Hexoskin
            print("Creating realtime request");
            currentTime = currentTime + 15;

            int currentHexoTime = currentTime * 256;
            int endHexoTime = endTime * 256;

            //Build request url
            //This request will return a flat array of values without timestamps for the requested datatype
            String realTimeReqURI = "https://api.hexoskin.com/api/data/?" + "user=" + USERID + "&datatype=18" + "&start=" + currentHexoTime + "&end=" + endHexoTime + "&flat=1&no_timestamps=exact";

            UnityWebRequest realTimeReq = UnityWebRequest.Get(realTimeReqURI);
            realTimeReq.SetRequestHeader("Authorization", "Bearer " + AUTHTOKEN);
            realTimeReq.chunkedTransfer = false;

            print("Submitting");
            yield return realTimeReq.SendWebRequest();
            print("Response received");

            //Check for errors
            if (realTimeReq.error == null)
            {
                print("Response Text");
                print(realTimeReq.downloadHandler.text);
                //Parse response for auth
                //ParseAuth(realTimeReq.downloadHandler.text);
            }
            else
            {
                print("Refresh error " + realTimeReq.error);
            }
            yield return new WaitForSeconds(15);
        }
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
			ParseAuth(refreshReq.downloadHandler.text);
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