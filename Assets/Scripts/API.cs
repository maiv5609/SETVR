using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft;
using System.Diagnostics;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class API : MonoBehaviour {
    public int USERID;
	private const string AUTHURL = "https://api.hexoskin.com/api/connect/oauth2/token/";
	private const string URL = "https://api.hexoskin.com/api/user";

	private string AUTHTOKEN;
	private string REFRESHTOKEN;

    private Stopwatch timeline = new Stopwatch(); //Timestamp tracking

    //Base times for requests
    private ulong startTime;
	private ulong currentTime;
	private ulong endTime;

	//Icons for Stress
	public Image RIcon; 
	public Image YIcon;
	public Image GIcon;

	void Start(){
        //Initial UI
        GIcon.CrossFadeAlpha(1, 1.0f, false);
        YIcon.CrossFadeAlpha(0, 0.1f, false);
        RIcon.CrossFadeAlpha(0, 0.1f, false);
        timeline.Start();
		Request();
    }

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

	public class RealtimeResponse
	{
		public string datatype { get; set; }
		public int[] data { get; set; }
	}

    //Main function
    public void Request(){
        //TODO: Remove this
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
		String filePath = Application.dataPath + "/Resources/Visualizations/RR.csv";
        String filePathB = Application.dataPath + "/Resources/Visualizations/Breathing.csv";

        StreamWriter writer = new StreamWriter(filePath);
        StreamWriter writerB = new StreamWriter(filePathB);

        writer.WriteLine("Time,RR Interval");
        writerB.WriteLine("Time,Breathing");

        //Set current timestamp for realtime request, need to multiply this by 256 before request
        TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
		ulong secondsSinceEpoch = (ulong)t.TotalSeconds;
		startTime = secondsSinceEpoch;
		currentTime = secondsSinceEpoch;
		float valueAvg;
		int numValues;

		//Get data from x seconds ago (Currently need to do testing beforehand to know the delay)
		currentTime = currentTime - 60;

		ulong currentHexoTime;
		ulong endHexoTime;

		endTime = startTime + 86400; //End time 24 hours from then
        endHexoTime = endTime * 256;
        //By this point valid auth token has been given Repeat call every 15 seconds
        //Have to use while true with a sleep in order to continuing calling every 15 seconds
        while (true){
			//Create and send auth request to Hexoskin
			//print("Creating realtime request");
			currentHexoTime = currentTime * 256;

            //Build request url 18
            //This request will return a flat array of values without timestamps for the requested datatype
            String realTimeReqURI = "https://api.hexoskin.com/api/data/?" + "user=" + USERID + "&datatype=18" + "&start=" + currentHexoTime.ToString() + "&end=" + endHexoTime.ToString();
            String realTimeBreathURI = "https://api.hexoskin.com/api/data/?" + "user=" + USERID + "&datatype=33" + "&start=" + currentHexoTime.ToString() + "&end=" + endHexoTime.ToString();
            //print (realTimeReqURI);
            //https://api.hexoskin.com/api/data/?user=14159&datatype=18&start=392579598592&end=392579602432&flat=1&no_timestamps=exact
            
            
            /*    Request for RR interval data     */
            using (UnityWebRequest realTimeReq = UnityWebRequest.Get (realTimeReqURI)) {
                //print (AUTHTOKEN);
                DownloadHandler dH = new DownloadHandlerBuffer();
                realTimeReq.downloadHandler = dH;
                realTimeReq.chunkedTransfer = false;
				realTimeReq.SetRequestHeader("Authorization", "Bearer " + AUTHTOKEN);
				yield return realTimeReq.SendWebRequest();

                print(realTimeReq.downloadedBytes);
				//Check for errors
				if (realTimeReq.isNetworkError || realTimeReq.isHttpError)
				{
					print("Refresh error " + realTimeReq.error);
					print(dH.text);
				}
				else{
					valueAvg = 0.0f;
					numValues = 0;

					string output = realTimeReq.downloadHandler.text;
                    print("Response: " + realTimeReq.downloadHandler.text);
                    string temp;
					Newtonsoft.Json.JsonReader reader = new Newtonsoft.Json.JsonTextReader(new StringReader(output));

                    bool next = false; //Track next value
                    double stampTemp = 0;
                    while (reader.Read ()) {
						if (reader.Value != null && reader.TokenType.ToString() == "Float") {
                            temp = reader.Value.ToString ();
                            if(!next && temp != "18") {
                                //Timestamp
                                stampTemp = ulong.Parse(temp);
                                stampTemp = stampTemp / 256;
                                stampTemp = stampTemp - startTime;
                                next = true;
                            }else if (next && temp != "18") {
                                if(stampTemp > 0.0){
                                    //If timestamp if within the simulation starting
                                    stampTemp = stampTemp / 60;
                                    writer.WriteLine(stampTemp + "," + temp);
                                    writer.Flush();
                                    valueAvg = valueAvg + float.Parse(temp);
                                    numValues++;
                                }
                                next = false;
                            }
						}
					}
					valueAvg = valueAvg / numValues;

					//Adjust HUD
					if(valueAvg <= 0.4){
                        //Red
                        GIcon.CrossFadeAlpha(0, 1.0f, false);
                        YIcon.CrossFadeAlpha(0, 1.0f, false);
                        RIcon.CrossFadeAlpha(1, 2.0f, false);
                    } else if (valueAvg > 0.4 && valueAvg <= 0.55){
                        //Yellow
                        GIcon.CrossFadeAlpha(0, 1.0f, false);
                        YIcon.CrossFadeAlpha(1, 2.0f, false);
                        RIcon.CrossFadeAlpha(0, 1.0f, false);
                    } else {
                        //Green
                        GIcon.CrossFadeAlpha(1, 2.0f, false);
                        YIcon.CrossFadeAlpha(0, 1.0f, false);
                        RIcon.CrossFadeAlpha(0, 1.0f, false);
                    }

					print (valueAvg);
             
					
				}
			}

            /*    Request for breathing data     */
            //TODO: Might need to use something rather than breathing rate as there are gaps in the data. But it mightbe ok. See if I can get raw breathing data.
            using (UnityWebRequest realTimeReq = UnityWebRequest.Get(realTimeBreathURI))
            {
                //print (AUTHTOKEN);
                DownloadHandler dH = new DownloadHandlerBuffer();
                realTimeReq.downloadHandler = dH;
                realTimeReq.chunkedTransfer = false;
                realTimeReq.SetRequestHeader("Authorization", "Bearer " + AUTHTOKEN);
                yield return realTimeReq.SendWebRequest();

                //Check for errors
                if (realTimeReq.isNetworkError || realTimeReq.isHttpError)
                {
                    print("Refresh error " + realTimeReq.error);
                    print(dH.text);
                }
                else
                {
                    string output = realTimeReq.downloadHandler.text;
                    print("Response: " + realTimeReq.downloadHandler.text);
                    string temp;
                    Newtonsoft.Json.JsonReader reader = new Newtonsoft.Json.JsonTextReader(new StringReader(output));

                    bool next = false; //Track next value
                    double stampTemp = 0;
                    string prevValue = null;
                    while (reader.Read())
                    {
                        if (reader.Value != null && reader.TokenType.ToString() == "Float")
                        {
                            temp = reader.Value.ToString();
                            if (!next && temp != "33")
                            {
                                //Timestamp
                                stampTemp = ulong.Parse(temp);
                                stampTemp = stampTemp / 256;
                                stampTemp = stampTemp - startTime;
                                next = true;
                            }
                            else if (next && temp != "33")
                            {
                                if (stampTemp > 0.0)
                                {
                                    //If timestamp if within the simulation starting
                                    stampTemp = stampTemp / 60;
                                    prevValue = temp;
                                    if(ulong.Parse(temp) > 10000)
                                    {
                                        if(ulong.Parse(prevValue) > 10000)
                                        {
                                            //if first value is null toss out

                                        }
                                        else
                                        {
                                            //If current value is null
                                            writerB.WriteLine(stampTemp + "," + prevValue);
                                            writerB.Flush();
                                        }
                                        
                                    }
                                    else
                                    {
                                        writerB.WriteLine(stampTemp + "," + temp);
                                        writerB.Flush();
                                    }

                                    
                                }
                                next = false;
                            }
                        }
                    }
                }
            }

            yield return new WaitForSeconds(60);
			currentTime = currentTime + 60;
		}
		writer.Close ();
	}

	void OnApplicationQuit(){
		
	}
}