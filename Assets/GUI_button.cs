using UnityEngine;
using System.Collections;
using NAudio.Wave;

public class GUI_button : MonoBehaviour {

	WaveInEvent waveInStream;
	WaveFileWriter writer;

	// Use this for initialization
	void Start () {
		waveInStream = new WaveInEvent();
		//writer = new WaveFileWriter("SynesthesiaOutput_" + System.DateTime.Now.Year.ToString() + "-" + System.DateTime.Now.Month.ToString() + "-" + System.DateTime.Now.Day.ToString() + "_" + System.DateTime.Now.Hour.ToString() + "-" + System.DateTime.Now.Minute.ToString() + "-" + System.DateTime.Now.Second.ToString() + ".wav", waveInStream.WaveFormat);
		//waveInStream.DataAvailable += new System.EventHandler<WaveInEventArgs>(waveInStream_DataAvailable);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void waveInStream_DataAvailable(object sender, WaveInEventArgs e)
	{
		writer.WriteData(e.Buffer, 0, e.BytesRecorded);
	}

	private void OnGUI()
	{
		
				if (GUI.Button (new Rect (15, 15, 60, 50), "Record")) {
						//Debug.Log ("start recording");
						waveInStream = new WaveInEvent ();
						writer = new WaveFileWriter ("SynesthesiaOutput_" + System.DateTime.Now.Year.ToString () + "-" + System.DateTime.Now.Month.ToString () + "-" + System.DateTime.Now.Day.ToString () + "_" + System.DateTime.Now.Hour.ToString () + "-" + System.DateTime.Now.Minute.ToString () + "-" + System.DateTime.Now.Second.ToString () + ".wav", waveInStream.WaveFormat);
						waveInStream.DataAvailable += new System.EventHandler<WaveInEventArgs> (waveInStream_DataAvailable);
						waveInStream.StartRecording ();
				}
		
				if (GUI.Button (new Rect (85, 15, 50, 50), "Stop")) {
						waveInStream.StopRecording ();
						waveInStream.Dispose ();
						waveInStream = null;
						writer.Close ();
						writer = null;
						//Debug.Log ("Stop Recording");
				}
		}
}