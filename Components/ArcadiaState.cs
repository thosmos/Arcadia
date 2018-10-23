﻿#if NET_4_6
using UnityEngine;
using System.Collections.Generic;
using clojure.lang;
using Arcadia;
using System;

[ExecuteInEditMode]
public class ArcadiaState : MonoBehaviour, ISerializationCallbackReceiver
{

	// TODO sorted maps?
	public string edn = "{}";
	//public JumpMap state = new JumpMap();
	public Dictionary<Keyword, object> state = new Dictionary<Keyword, object>();

	public Atom objectDatabase = null;
	public int[] objectDatabaseIds = new int[0];
	public UnityEngine.Object[] objectDatabaseObjects = new UnityEngine.Object[0];

	private static IFn prStr = null;
	private static IFn readString = null;
	private static IFn requireFn = null;

	[System.NonSerialized]
	public bool fullyInitialized = false;

	// creates objectDatabase atom from
	// objectDatabaseIds and objectDatabaseObjects
	public void BuildDatabaseAtom (bool force = false)
	{
		if (objectDatabase == null || force) {
			var idsToObjectsMap = PersistentHashMap.EMPTY;

			if (objectDatabaseIds.Length > 0 && objectDatabaseObjects.Length > 0) {
				// TODO transients?
				int len = System.Math.Min(objectDatabaseIds.Length, objectDatabaseObjects.Length);
				for (int i = 0; i < len; i++) {
					idsToObjectsMap = (PersistentHashMap)idsToObjectsMap.assoc(objectDatabaseIds[i], objectDatabaseObjects[i]);
				}
			}

			objectDatabase = new Atom(idsToObjectsMap);
		}
	}

	void WipeDatabase ()
	{
		objectDatabase = new Atom(PersistentHashMap.EMPTY);
	}

	// =====================================================
	// Static Data

	public static Var dataReaders;

	public static Var awakeFn;

	public static Var jumpMapToMapVar;

	public static Var deserializeVar;

	public static Var objectDbVar;

	public static Var serializeVar;

	public static Var printReadablyVar;

	public static Var prStrVar;

	public static bool varsInitialized = false;

	// =====================================================

	private static void InitializeOwnVars ()
	{
		if (varsInitialized)
			return;

		Arcadia.Util.require("arcadia.data"); // side-effects clojure.core/*data-readers*
		Arcadia.Util.getVar(ref dataReaders, "clojure.core", "*data-readers*");

		string stateHelpNs = "arcadia.internal.state-help";
		Arcadia.Util.require(stateHelpNs);
		Arcadia.Util.getVar(ref awakeFn, stateHelpNs, "awake");
		Arcadia.Util.getVar(ref jumpMapToMapVar, stateHelpNs, "jumpmap-to-map");
		Arcadia.Util.getVar(ref deserializeVar, stateHelpNs, "deserialize");

		var arcadiaLiteralsNs = "arcadia.data";
		Arcadia.Util.require(arcadiaLiteralsNs);
		Arcadia.Util.getVar(ref serializeVar, arcadiaLiteralsNs, "*serialize*");
		Arcadia.Util.getVar(ref objectDbVar, arcadiaLiteralsNs, "*object-db*");

		var coreNs = "clojure.core";
		Arcadia.Util.getVar(ref printReadablyVar, coreNs, "*print-readably*");
		Arcadia.Util.getVar(ref prStrVar, coreNs, "pr-str");

		varsInitialized = true;
	}


	// Require vars and full deserialize.
	// Will eventually call GetComponent via RefreshAll, so can't be called during OnAfterDeserialize
	// Triggered by Awake, also by FullInit in ArcadiaBehaviour
	public void Initialize ()
	{
		if (fullyInitialized)
			return;
		
		// TODO: cache component access
		foreach (var ab in GetComponents<ArcadiaBehaviour>()) {
			ab.RealizeVars();
		}

		InitializeOwnVars();
		deserializeVar.invoke(this);
		fullyInitialized = true;
	}

	public void Awake ()
	{
		Initialize();
	}

	public void OnBeforeSerialize ()
	{
		Initialize();

		WipeDatabase();
		Var.pushThreadBindings(RT.map(objectDbVar, objectDatabase, serializeVar, true, printReadablyVar, false));
		try {
			//edn = (string)prStrVar.invoke(jumpMapToMapVar.invoke(state)); // side effects, updating objectDatabase
			edn = (string)prStrVar.invoke(Arcadia.Util.DictionaryToMap(state));
			// TODO optimize this
			var map = (PersistentHashMap)objectDatabase.deref();
			objectDatabaseIds = (int[])RT.seqToTypedArray(typeof(int), RT.keys(map));
			objectDatabaseObjects = (UnityEngine.Object[])RT.seqToTypedArray(typeof(UnityEngine.Object), RT.vals(map));
		} finally {
			Var.popThreadBindings();
		}
	}

	// need for ISerializationCallbackReceiver interface
	public void OnAfterDeserialize ()
	{
	
	}

	void OnDestroy ()
	{
		if (ReferenceEquals(HookStateSystem.arcadiaState, this)) {
			HookStateSystem.hasState = false;
		}
	}

	// ============================================================
	// retrieval

	public clojure.lang.IPersistentMap ToPersistentMap ()
	{
		return Arcadia.Util.DictionaryToMap<Keyword,object>(state);
	}

	// ============================================================
	// modification
	//public void RefreshAll ()
	//{
	//	var arcadiaBehaviours = gameObject.GetComponents<ArcadiaBehaviour>();
	//	for (var i = 0; i < arcadiaBehaviours.Length; i++) {
	//		arcadiaBehaviours[i].RefreshPamvs();
	//	}
	//}

	public void Add (object k, object v)
	{
		//bool hadKey = state.ContainsKey(k);
		state.Add((Keyword)k, v);
		// determine if this warrants refreshing the pamv's
		//if (!hadKey) {
		//	RefreshAll();
		//}
	}

	public void Remove (object k)
	{
		state.Remove((Keyword)k);
		// don't need to refresh anything
	}

	public void Clear ()
	{
		state.Clear();
	}

	// TODO add arity with default value
	public object ValueAtKey (object k)
	{
		//return state.ValueAtKey(k);
		object v;
		if (state.TryGetValue((Keyword)k, out v)) {
			return v;
		}
		return null;
	}

	//public JumpMap.PartialArrayMapView pamv (object[] ks)
	//{
	//	Debug.Log("In ArcadiaState.pamv. ks.Length:" + ks.Length);
	//	return state.pamv(ks);
	//}


}
#endif