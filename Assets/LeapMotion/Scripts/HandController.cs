/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using Leap;
using System.Collections.Generic;
using CSharpSynth.Effects;
using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using CSharpSynth.Midi;

// Overall Controller object that will instantiate hands and tools when they appear.
[RequireComponent (typeof(AudioSource))]
public class HandController : MonoBehaviour {

  // Reference distance from thumb base to pinky base in mm.
  protected const float GIZMO_SCALE = 5.0f;
  protected const float MM_TO_M = 0.001f;

  public bool separateLeftRight = false;
  public HandModel leftGraphicsModel;
  public HandModel leftPhysicsModel;
  public HandModel rightGraphicsModel;
  public HandModel rightPhysicsModel;

  public ToolModel toolModel;

  public bool mirrorZAxis = false;

  // If hands are in charge of Destroying themselves, make this false.
  public bool destroyHands = true;

  public Vector3 handMovementScale = Vector3.one;

  // Recording parameters.
  public bool enableRecordPlayback = false;
  public TextAsset recordingAsset;
  public float recorderSpeed = 1.0f;
  public bool recorderLoop = true;
  
  private LeapRecorder recorder_ = new LeapRecorder();
  
  private Controller leap_controller_;

  private Dictionary<int, HandModel> hand_graphics_;
  private Dictionary<int, HandModel> hand_physics_;
  private Dictionary<int, ToolModel> tools_;

	//Public
	//Check the Midi's file folder for different songs
	public string midiFilePath = "Midis/Groove.mid";
	//Try also: "FM Bank/fm" or "Analog Bank/analog" for some different sounds
	public string bankFilePath = "GM Bank/gm";
	public int bufferSize = 1024;
	public int midiNote = 60;
	public int midiNoteVolume = 100;
	public int midiInstrument = 89;
	//Private 
	private float[] sampleBuffer;
	private float gain = 1f;
	private MidiSequencer midiSequencer;
	private StreamSynthesizer midiStreamSynthesizer;
	
	private float sliderValue = 1.0f;
	private float maxSliderValue = 127.0f;

	private int prevHandXVelocity = 0;
  
  void OnDrawGizmos() {
    Gizmos.matrix = Matrix4x4.Scale(GIZMO_SCALE * Vector3.one);
    Gizmos.DrawIcon(transform.position, "leap_motion.png");
  }

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

  void Start() {
    leap_controller_ = new Controller();
    hand_graphics_ = new Dictionary<int, HandModel>();
    hand_physics_ = new Dictionary<int, HandModel>();

    tools_ = new Dictionary<int, ToolModel>();

    if (leap_controller_ == null) {
      Debug.LogWarning(
          "Cannot connect to controller. Make sure you have Leap Motion v2.0+ installed");
    }

    if (enableRecordPlayback && recordingAsset != null)
      recorder_.Load(recordingAsset);
  }

  private void IgnoreCollisions(GameObject first, GameObject second, bool ignore = true) {
    if (first == null || second == null)
      return;

    Collider[] first_colliders = first.GetComponentsInChildren<Collider>();
    Collider[] second_colliders = second.GetComponentsInChildren<Collider>();

    for (int i = 0; i < first_colliders.Length; ++i) {
      for (int j = 0; j < second_colliders.Length; ++j)
        Physics.IgnoreCollision(first_colliders[i], second_colliders[j], ignore);
    }
  }

  private void IgnoreCollisionsWithChildren(GameObject to_ignore) {
    IgnoreCollisions(gameObject, to_ignore);
  }

  public void IgnoreCollisionsWithHands(GameObject to_ignore, bool ignore = true) {
    foreach (HandModel hand in hand_physics_.Values)
      IgnoreCollisions(hand.gameObject, to_ignore, ignore);
  }

  private HandModel CreateHand(HandModel model) {
    HandModel hand_model = Instantiate(model, transform.position, transform.rotation)
                           as HandModel;
    hand_model.gameObject.SetActive(true);
    IgnoreCollisionsWithChildren(hand_model.gameObject);
    return hand_model;
  }

  private void DestroyHand(HandModel hand_model) {
    if (destroyHands)
      Destroy(hand_model.gameObject);
    else
      hand_model.SetLeapHand(null);
  }

  private void UpdateHandModels(Dictionary<int, HandModel> all_hands,
                                HandList leap_hands,
                                HandModel left_model, HandModel right_model) {
    List<int> ids_to_check = new List<int>(all_hands.Keys);

    // Go through all the active hands and update them.
    int num_hands = leap_hands.Count;
    for (int h = 0; h < num_hands; ++h) {
      Hand leap_hand = leap_hands[h];
      
      HandModel model = (mirrorZAxis != leap_hand.IsLeft) ? left_model : right_model;

      // If we've mirrored since this hand was updated, destroy it.
      if (all_hands.ContainsKey(leap_hand.Id) &&
          all_hands[leap_hand.Id].IsMirrored() != mirrorZAxis) {
        DestroyHand(all_hands[leap_hand.Id]);
        all_hands.Remove(leap_hand.Id);
      }

      // Only create or update if the hand is enabled.
      if (model != null) {
        ids_to_check.Remove(leap_hand.Id);

        // Create the hand and initialized it if it doesn't exist yet.
        if (!all_hands.ContainsKey(leap_hand.Id)) {
          HandModel new_hand = CreateHand(model);
          new_hand.SetLeapHand(leap_hand);
          new_hand.MirrorZAxis(mirrorZAxis);
          new_hand.SetController(this);

          // Set scaling based on reference hand.
          float hand_scale = MM_TO_M * leap_hand.PalmWidth / new_hand.handModelPalmWidth;
          new_hand.transform.localScale = hand_scale * transform.localScale;

          new_hand.InitHand();
          new_hand.UpdateHand();
          all_hands[leap_hand.Id] = new_hand;
        }
        else {
          // Make sure we update the Leap Hand reference.
          HandModel hand_model = all_hands[leap_hand.Id];
          hand_model.SetLeapHand(leap_hand);
          hand_model.MirrorZAxis(mirrorZAxis);

          // Set scaling based on reference hand.
          float hand_scale = MM_TO_M * leap_hand.PalmWidth / hand_model.handModelPalmWidth;
          hand_model.transform.localScale = hand_scale * transform.localScale;
          hand_model.UpdateHand();
        }
      }
    }

    // Destroy all hands with defunct IDs.
    for (int i = 0; i < ids_to_check.Count; ++i) {
      DestroyHand(all_hands[ids_to_check[i]]);
      all_hands.Remove(ids_to_check[i]);
    }
  }

  private ToolModel CreateTool(ToolModel model) {
    ToolModel tool_model = Instantiate(model, transform.position, transform.rotation)
                           as ToolModel;
    tool_model.gameObject.SetActive(true);
    IgnoreCollisionsWithChildren(tool_model.gameObject);
    return tool_model;
  }

  private void UpdateToolModels(Dictionary<int, ToolModel> all_tools,
                                ToolList leap_tools, ToolModel model) {
    List<int> ids_to_check = new List<int>(all_tools.Keys);

    // Go through all the active tools and update them.
    int num_tools = leap_tools.Count;
    for (int h = 0; h < num_tools; ++h) {
      Tool leap_tool = leap_tools[h];
      
      // Only create or update if the tool is enabled.
      if (model) {

        ids_to_check.Remove(leap_tool.Id);

        // Create the tool and initialized it if it doesn't exist yet.
        if (!all_tools.ContainsKey(leap_tool.Id)) {
          ToolModel new_tool = CreateTool(model);
          new_tool.SetController(this);
          new_tool.SetLeapTool(leap_tool);
          new_tool.InitTool();
          all_tools[leap_tool.Id] = new_tool;
        }

        // Make sure we update the Leap Tool reference.
        ToolModel tool_model = all_tools[leap_tool.Id];
        tool_model.SetLeapTool(leap_tool);
        tool_model.MirrorZAxis(mirrorZAxis);

        // Set scaling.
        tool_model.transform.localScale = transform.localScale;

        tool_model.UpdateTool();
      }
    }

    // Destroy all tools with defunct IDs.
    for (int i = 0; i < ids_to_check.Count; ++i) {
      Destroy(all_tools[ids_to_check[i]].gameObject);
      all_tools.Remove(ids_to_check[i]);
    }
  }

  Frame GetFrame() {
    if (enableRecordPlayback && recorder_.state == RecorderState.Playing)
      return recorder_.GetCurrentFrame();

    return leap_controller_.Frame();
  }

  void Update() {
    if (leap_controller_ == null)
      return;
    
    UpdateRecorder();
    Frame frame = GetFrame();
    UpdateHandModels(hand_graphics_, frame.Hands, leftGraphicsModel, rightGraphicsModel);
		if (Input.GetKeyDown(KeyCode.D)) {
			midiInstrument += 1;
			if(midiInstrument > 127)
				midiInstrument = 1;
			midiStreamSynthesizer.NoteOff (1, midiNote);
			midiNote = -1;
		}
		if (Input.GetKeyDown(KeyCode.A)) {
			midiInstrument -= 1;
			if (midiInstrument < 1)
				midiInstrument = 127;
			midiStreamSynthesizer.NoteOff (1, midiNote);
			midiNote = -1;
		}
		Hand rh = frame.Hands.Rightmost;
		Vector position = rh.PalmPosition;
		Vector velocity = rh.PalmVelocity;
		Vector direction = rh.Direction;
		int velocity_x = (int)velocity.x;
		//int midiNote2 = (int)((position.x + 275.0) / 6.0);
	    //midiNoteVolume = (int)(-(position.z - 275.0) / 5.5); 

		midiNoteVolume = (int)velocity.Magnitude / 5;
		midiStreamSynthesizer.setVolume (1, midiNoteVolume);


		if(((velocity_x >> 31) & 0x1) != ((prevHandXVelocity >> 31) &0x1)){
			midiStreamSynthesizer.NoteOff (1, midiNote);
			midiNote = (int)((position.y + 275.0) / 6.0);
			midiStreamSynthesizer.NoteOn (1, midiNote, midiNoteVolume, midiInstrument);

		}
		Debug.Log ("Velocity Magnitude is: " + velocity.ToString ());
		/*if ((int)midiNote2 != midiNote) {
			midiStreamSynthesizer.NoteOff (1, midiNote);
			midiNote = (int)midiNote2;
			midiStreamSynthesizer.NoteOn (1, midiNote, midiNoteVolume, midiInstrument);
		}*/
		prevHandXVelocity = (int)velocity.x;
  }

  void FixedUpdate() {
    if (leap_controller_ == null)
      return;

    Frame frame = GetFrame();
    UpdateHandModels(hand_physics_, frame.Hands, leftPhysicsModel, rightPhysicsModel);
    UpdateToolModels(tools_, frame.Tools, toolModel);
	
  }

  public float GetRecordingProgress() {
    return recorder_.GetProgress();
  }

  public void StopRecording() {
    recorder_.Stop();
  }

  public void PlayRecording() {
    recorder_.Play();
  }

  public void PauseRecording() {
    recorder_.Pause();
  }

  public string FinishAndSaveRecording() {
    string path = recorder_.SaveToNewFile();
    recorder_.Play();
    return path;
  }

  public void ResetRecording() {
    recorder_.Reset();
  }

  public void Record() {
    recorder_.Record();
  }

  public bool IsConnected() {
    return leap_controller_.IsConnected;
  }

  public bool IsEmbedded() {
    DeviceList devices = leap_controller_.Devices;
    if (devices.Count == 0)
      return false;
    return devices[0].IsEmbedded;
  }

  void UpdateRecorder() {
    if (!enableRecordPlayback)
      return;

    recorder_.speed = recorderSpeed;
    recorder_.loop = recorderLoop;

    if (recorder_.state == RecorderState.Recording) {
      recorder_.AddFrame(leap_controller_.Frame());
    }
    else {
      recorder_.NextFrame();
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
