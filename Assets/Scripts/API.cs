using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft;
using System.IO;

using UnityEngine;
using UnityEngine.Networking;

//TODO: Work on realtime request and switching starting timestamp to last available timestamp from previous request

public class API : MonoBehaviour {
    public int USERID;
	private const string AUTHURL = "https://api.hexoskin.com/api/connect/oauth2/token/";
	private const string URL = "https://api.hexoskin.com/api/user";

	public string AUTHTOKEN;
	public string REFRESHTOKEN;

    //Base times for requests
	private ulong startTime;
	private ulong currentTime;
	private ulong endTime;

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
//		TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
//		ulong secondsSinceEpoch = (ulong)t.TotalSeconds;
//		startTime = secondsSinceEpoch;
//		currentTime = secondsSinceEpoch;
//
//		print (currentTime);
//		print (currentTime * 256);

        StartCoroutine(RequestToken());
    }

    /* Requesting Functions */

    /* Requests new access token
	 * 
	 */
    private IEnumerator RequestToken(){
		bool auth = false;

		//Create and send auth request to Hexoskin
		//print("Creating auth request");
		WWWForm refreshForm = new WWWForm ();
		refreshForm.AddField("username", "webert3@wwu.edu");
		refreshForm.AddField("password", "neat_lab_2018");
		refreshForm.AddField("scope", "readonly");
		refreshForm.AddField("grant_type", "password");

		using (UnityWebRequest refreshReq = UnityWebRequest.Post (AUTHURL, refreshForm)) {
			refreshReq.SetRequestHeader("Authorization", "Basic Y0JjaXdYY0pYZlljNURsNWNRMnJid0Z1bVljbVFMOjdkNjE4dE1ydnNRSDJOVzhoWlBybDJyNk9USEttaw==");
			refreshReq.chunkedTransfer = false;

			//print("Submitting");
			yield return refreshReq.SendWebRequest();
			//Check for errors
			if (refreshReq.error == null){
				//print("Auth Success");
				print(refreshReq.downloadHandler.text);
				//Parse response for auth
				AuthResponse tokens = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthResponse>(refreshReq.downloadHandler.text);
				AUTHTOKEN = tokens.access_token;
				print ("Parsed: " + AUTHTOKEN);
				auth = true;
			}
			else{
				print("Refresh error " + refreshReq.error);
			}
		}
		if (auth) {
			StartCoroutine (RealTimeRequest ());
		}
	}

	/* Makes a request for realtime data
     * Called with the assumption that there is a valid auth token
     * Requests should ask for 15 seconds of data every 15 seconds
     * https://api.hexoskin.com/api/data/?user=14052&datatype=18&start=392531420416&end=392541193472&flat=1&no_timestamps=exact
     */
	private IEnumerator RealTimeRequest(){
		String filePath = Application.dataPath + "RR.csv";
		StreamWriter writer = new StreamWriter (filePath);
		//Set current timestamp for realtime request, need to multiply this by 256 before request
		TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
		ulong secondsSinceEpoch = (ulong)t.TotalSeconds;
		startTime = secondsSinceEpoch;
		currentTime = secondsSinceEpoch;

		//Get data from 30 seconds ago
		startTime = startTime - 15;
		currentTime = currentTime - 15;

		ulong currentHexoTime;
		ulong endHexoTime;

		endTime = startTime + 86400; //End time 24 hours from then

		//By this point valid auth token has been given Repeat call every 15 seconds
		//Have to use while true with a sleep in order to continuing calling every 15 seconds
		while (true){
			//Create and send auth request to Hexoskin
			//print("Creating realtime request");
			currentHexoTime = currentTime * 256;
			endHexoTime = endTime * 256;

			//Build request url 18
			//This request will return a flat array of values without timestamps for the requested datatype
			String realTimeReqURI = "https://api.hexoskin.com/api/data/?" + "user=" + USERID + "&datatype=18" + "&start=" + currentHexoTime + "&end=" + endHexoTime + "&no_timestamps=exact";
			print (realTimeReqURI);
			//https://api.hexoskin.com/api/data/?user=14159&datatype=18&start=392579598592&end=392579602432&flat=1&no_timestamps=exact
			//"
			using (UnityWebRequest realTimeReq = UnityWebRequest.Get (realTimeReqURI)) {
				//print (AUTHTOKEN);
				realTimeReq.SetRequestHeader("Authorization", "Bearer " + AUTHTOKEN);

				yield return realTimeReq.SendWebRequest();

				//Check for errors
				if (realTimeReq.isNetworkError || realTimeReq.isHttpError)
				{
					print("Refresh error " + realTimeReq.error);
					print(realTimeReq.downloadHandler.text);
				}
				else{
					//  print("Response Text");
					print(realTimeReq.downloadHandler.text);
					string output = realTimeReq.downloadHandler.text;
					print ("Write: " + output);
					writer.WriteLine (output);
					writer.Flush ();
				}
			}

			yield return new WaitForSeconds(16);
			currentTime = currentTime + 15;
		}
		writer.Close ();
	}

	void OnApplicationQuit(){
		
	}
}