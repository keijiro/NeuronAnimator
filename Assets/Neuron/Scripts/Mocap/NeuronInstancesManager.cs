/************************************************************************************
 Copyright: Copyright 2014 Beijing Noitom Technology Ltd. All Rights reserved.
 Pending Patents: PCT/CN2014/085659 PCT/CN2014/071006

 Licensed under the Perception Neuron SDK License Beta Version (the “License");
 You may only use the Perception Neuron SDK when in compliance with the License,
 which is provided at the time of installation or download, or which
 otherwise accompanies this software in the form of either an electronic or a hard copy.

 A copy of the License is included with this package or can be obtained at:
 http://www.neuronmocap.com

 Unless required by applicable law or agreed to in writing, the Perception Neuron SDK
 distributed under the License is provided on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing conditions and
 limitations under the License.
************************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using Neuron;

public class NeuronInstancesManager : MonoBehaviour
{
	public GameObject 									replicaPrefab = null;
	public int											numOfInstances { get { return instances.Count; } }
	public int											numOfReserveInstances = 0;
	
	List<NeuronSource>									sources = new List<NeuronSource>();
	List<NeuronActor>									actorsToInstantiate = new List<NeuronActor>();
	List<NeuronAnimatorInstance>						instancesToDestroy = new List<NeuronAnimatorInstance>();
	
	Dictionary<NeuronActor, NeuronAnimatorInstance>		instances = new Dictionary<NeuronActor, NeuronAnimatorInstance>();
	List<NeuronAnimatorInstance>						reservedInstances = new List<NeuronAnimatorInstance>();
	
	bool ValidatePrefab()
	{
		if( replicaPrefab != null )
		{
			return replicaPrefab.GetComponent<NeuronAnimatorInstance>() != null;
		}
		
		return false;
	}
	
	void OnResumeActor( NeuronActor actor )
	{
		actorsToInstantiate.Add( actor );
	}
	
	void OnSuspendActor( NeuronActor actor )
	{
		NeuronAnimatorInstance instance = null;
		instances.TryGetValue( actor, out instance );
		if( instance != null )
		{
			if( instances.Count > numOfReserveInstances )
			{
				instancesToDestroy.Add( instance );	
			}
			else
			{
				reservedInstances.Add( instance );
				instances.Remove( actor );
			}
		}
	}
	
	void ClearAllInstances()
	{
		for( int i = 0; i < reservedInstances.Count; ++i )
		{
			instancesToDestroy.Add( reservedInstances[i] );
		}
		reservedInstances.Clear();
	}
	
	public bool Connect( string address, int port, int commandServerPort, NeuronConnection.SocketType socketType )
	{
		NeuronSource source =  NeuronConnection.Connect( address, port, commandServerPort, socketType );
		if( source != null )
		{
			source.RegisterResumeActorCallback( OnResumeActor );
			source.RegisterSuspendActorCallback( OnSuspendActor );
			sources.Add( source );
		}
		return true;
	}
	
	public void Disconnect( string address, int port )
	{
		NeuronSource source = null;
		for( int i = 0; i < sources.Count; ++i )
		{
			if( address == sources[i].address && port == sources[i].port )
			{
				source = sources[i];
				break;
			}
		}

		if( source != null )
		{
			sources.Remove( source );
			OnDisconnect( source );
			NeuronConnection.Disconnect( source );
		}
		
		ClearAllInstances();
	}
	
	public void DiconnectAll()
	{
		for( int i = 0; i < sources.Count; ++i )
		{
			NeuronSource source = sources[i];
			OnDisconnect( source );
			NeuronConnection.Disconnect( source );
		}
		sources.Clear();
		
		ClearAllInstances();
	}
	
	public void OnDisconnect( NeuronSource source )
	{		
		NeuronActor[] activeActors = source.GetActiveActors();
		
		for( int i = 0; i < activeActors.Length; ++i )
		{
			NeuronActor actor = activeActors[i];
			NeuronAnimatorInstance instance = null;
			instances.TryGetValue( actor, out instance );
			if( instance != null )
			{
				instancesToDestroy.Add( instance );
			}
		}
	}
	
	public NeuronSource FindSource( string address, int port )
	{
		for( int i = 0; i < sources.Count; ++i )
		{
			if( sources[i].address == address && sources[i].port == port )
			{
				return sources[i];
			}
		}
		
		return null;
	}
	
	public NeuronAnimatorInstance[] GetInstances()
	{
		NeuronAnimatorInstance[] ret = new NeuronAnimatorInstance[instances.Count];
		instances.Values.CopyTo( ret, 0 );
		return ret;
	}
	
	bool SpawnInstance( NeuronActor actor )
	{
		if( actor == null || !ValidatePrefab() )
		{
			return false;
		}
		
		NeuronAnimatorInstance instance = null;
		if( reservedInstances.Count == 0 )
		{
			GameObject obj = GameObject.Instantiate( replicaPrefab, Vector3.zero, Quaternion.identity ) as GameObject;
			instance = obj.GetComponent<NeuronAnimatorInstance>();
			Debug.Log( string.Format( "[NeuronInstancesManager] Bound instance {0} actor {1}", obj.name, actor.guid.ToString( "N" ) ) );
			instance.SetBoundActor( actor );
		}
		else
		{
			instance = reservedInstances[reservedInstances.Count - 1];
			reservedInstances.RemoveAt( reservedInstances.Count - 1 );
			Debug.Log( string.Format( "[NeuronInstancesManager] Bound instance {0} actor {1}", instance.gameObject.name, actor.guid.ToString( "N" ) ) );
			instance.SetBoundActor( actor );
		}
		
		if( instance != null )
		{
			instances.Add( actor, instance );
		}
		
		return true;
	}
	
	void KillInstance( NeuronAnimatorInstance instance )
	{
		instances.Remove( instance.GetActor() );
		GameObject.Destroy( instance.gameObject );
	}
	
	public void Update()
	{
		NeuronConnection.OnUpdate();
	
		for( int i = 0; i < actorsToInstantiate.Count; ++i )
		{
			SpawnInstance( actorsToInstantiate[i] );
		}
		actorsToInstantiate.Clear();
		
		for( int i = 0; i < instancesToDestroy.Count; ++i )
		{
			KillInstance( instancesToDestroy[i] );
		}
		instancesToDestroy.Clear();
	}
	
	public void OnApplicationQuit()
	{
		for( int i = 0; i < sources.Count; ++i )
		{
			NeuronConnection.Disconnect( sources[i] );
		}
	}
}