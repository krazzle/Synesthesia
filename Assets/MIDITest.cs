using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CSharpSynth.Effects;
using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using CSharpSynth.Midi;

[RequireComponent (typeof(AudioSource))]
public class MIDITest : MonoBehaviour {

	//Public
	//Check the Midi's file folder for different songs
	public string midiFilePath = "Midis/Groove.mid";
	//Try also: "FM Bank/fm" or "Analog Bank/analog" for some different sounds
	public string bankFilePath = "GM Bank/gm";
	public int bufferSize = 1024;
	public int midiNote = 60;
	public int midiNoteVolume = 100;
	public int midiInstrument = 1;
	//Private 
	private float[] sampleBuffer;
	private float gain = 1f;
	private MidiSequencer midiSequencer;
	private StreamSynthesizer midiStreamSynthesizer;
	
	private float sliderValue = 1.0f;
	private float maxSliderValue = 127.0f;
	
	// Awake is called when the script instance
	// is being loaded.
	void Awake ()
	{
		midiStreamSynthesizer = new StreamSynthesizer (44100, 2, bufferSize, 40);
		sampleBuffer = new float[midiStreamSynthesizer.BufferSize];		
		
		midiStreamSynthesizer.LoadBank (bankFilePath);
		
		midiSequencer = new MidiSequencer (midiStreamSynthesizer);
		midiSequencer.LoadMidi (midiFilePath, false);
		//These will be fired by the midiSequencer when a song plays. Check the console for messages
		midiSequencer.NoteOnEvent += new MidiSequencer.NoteOnEventHandler (MidiNoteOnHandler);
		midiSequencer.NoteOffEvent += new MidiSequencer.NoteOffEventHandler (MidiNoteOffHandler);	
		
	}
	
	// Start is called just before any of the
	// Update methods is called the first time.
	void Start ()
	{
		
	}
	
	// Update is called every frame, if the
	// MonoBehaviour is enabled.
	void Update ()
	{
		//Demo of direct note output
		if (Input.GetKeyDown(KeyCode.A))
			midiStreamSynthesizer.NoteOn (1, midiNote, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.A))
			midiStreamSynthesizer.NoteOff (1, midiNote);
		if (Input.GetKeyDown(KeyCode.W))
			midiStreamSynthesizer.NoteOn (1, midiNote + 1, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.W))
			midiStreamSynthesizer.NoteOff (1, midiNote + 1);
		if (Input.GetKeyDown(KeyCode.S))
			midiStreamSynthesizer.NoteOn (1, midiNote + 2, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.S))
			midiStreamSynthesizer.NoteOff (1, midiNote + 2);		
		if (Input.GetKeyDown(KeyCode.E))
			midiStreamSynthesizer.NoteOn (1, midiNote + 3, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.E))
			midiStreamSynthesizer.NoteOff (1, midiNote + 3);
		if (Input.GetKeyDown(KeyCode.D))
			midiStreamSynthesizer.NoteOn (1, midiNote + 4, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.D))
			midiStreamSynthesizer.NoteOff (1, midiNote + 4);
		if (Input.GetKeyDown(KeyCode.F))
			midiStreamSynthesizer.NoteOn (1, midiNote + 5, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.F))
			midiStreamSynthesizer.NoteOff (1, midiNote + 5);
		if (Input.GetKeyDown(KeyCode.T))
			midiStreamSynthesizer.NoteOn (1, midiNote + 6, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.T))
			midiStreamSynthesizer.NoteOff (1, midiNote + 6);
		if (Input.GetKeyDown(KeyCode.G))
			midiStreamSynthesizer.NoteOn (1, midiNote + 7, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.G))
			midiStreamSynthesizer.NoteOff (1, midiNote + 7);		
		if (Input.GetKeyDown(KeyCode.Y))
			midiStreamSynthesizer.NoteOn (1, midiNote + 8, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.Y))
			midiStreamSynthesizer.NoteOff (1, midiNote + 8);
		if (Input.GetKeyDown(KeyCode.H))
			midiStreamSynthesizer.NoteOn (1, midiNote + 9, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.H))
			midiStreamSynthesizer.NoteOff (1, midiNote + 9);
		if (Input.GetKeyDown(KeyCode.U))
			midiStreamSynthesizer.NoteOn (1, midiNote + 10, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.U))
			midiStreamSynthesizer.NoteOff (1, midiNote + 10);
		if (Input.GetKeyDown(KeyCode.J))
			midiStreamSynthesizer.NoteOn (1, midiNote + 11, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.J))
			midiStreamSynthesizer.NoteOff (1, midiNote + 11);		
		if (Input.GetKeyDown(KeyCode.K))
			midiStreamSynthesizer.NoteOn (1, midiNote + 12, midiNoteVolume, midiInstrument);
		if (Input.GetKeyUp(KeyCode.K))
			midiStreamSynthesizer.NoteOff (1, midiNote + 12);		
		if (Input.GetKeyDown(KeyCode.Q))
			midiStreamSynthesizer.NoteOn (1, midiNote + 12, midiNoteVolume, midiInstrument);
		if (Input.GetKeyDown("up")) {
			midiInstrument += 1;
			if(midiInstrument > 127)
				midiInstrument = 1;
		}
		if (Input.GetKeyDown("down")) {
			midiInstrument -= 1;
			if (midiInstrument < 1)
			   midiInstrument = 127;
		}

		if (Input.GetKeyDown ("left")) {
			if (midiNoteVolume > 0)
				midiNoteVolume -= 10;
		}
		if (Input.GetKeyDown ("right")) {
			if (midiNoteVolume < 100)
				midiNoteVolume += 10;
		}


		

	}
	private void OnAudioFilterRead (float[] data, int channels)
	{
		
		//This uses the Unity specific float method we added to get the buffer
		midiStreamSynthesizer.GetNext (sampleBuffer);
		
		for (int i = 0; i < data.Length; i++) {
			data [i] = sampleBuffer [i] * gain;
		}
	}
	
	public void MidiNoteOnHandler (int channel, int note, int velocity)
	{
		Debug.Log ("NoteOn: " + note.ToString () + " Velocity: " + velocity.ToString ());
	}
	
	public void MidiNoteOffHandler (int channel, int note)
	{
		Debug.Log ("NoteOff: " + note.ToString ());
	}
}
