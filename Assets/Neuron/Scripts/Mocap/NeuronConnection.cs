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
using NeuronDataReaderWraper;

namespace Neuron
{
	public static class NeuronConnection
	{
		public enum SocketType
		{
			TCP,
			UDP
		}
		
		static Dictionary<Guid, NeuronSource>				connections = new Dictionary<Guid, NeuronSource>();
		static Dictionary<IntPtr, NeuronSource>				socketReferencesIndex = new Dictionary<IntPtr, NeuronSource>();
		static Dictionary<IntPtr, NeuronSource>				commandSocketReferenceIndex = new Dictionary<IntPtr, NeuronSource>();
		
		public static int									numOfSources { get { return connections.Count; } }
		
		public static NeuronSource Connect( string address, int port, int commandServerPort, SocketType socketType )
		{
			NeuronSource source = FindConnection( address, port, socketType );
			if( source != null )
			{
				source.Grab();
				return source;
			}
			
			source = CreateConnection( address, port, commandServerPort, socketType );
			if( source != null )
			{
				source.Grab();
				return source;
			}
			
			return null;
		}
		
		public static void Disconnect( NeuronSource source )
		{
			if( source != null )
			{
				source.Release();
				if( source.referenceCounter == 0 )
				{
					DestroyConnection( source );
				}
			}
		}
		
		public static void OnUpdate()
		{
			foreach( KeyValuePair<Guid, NeuronSource> it in connections )
			{
				it.Value.OnUpdate();
			}
		}
		
		public static NeuronSource[] GetSources()
		{
			NeuronSource[] sources = new NeuronSource[connections.Count];
			connections.Values.CopyTo( sources, 0 );
			return sources;
		}
		
		static NeuronSource CreateConnection( string address, int port, int commandServerPort, SocketType socketType )
		{	
			NeuronSource source = null;
			IntPtr socketReference = IntPtr.Zero;
			IntPtr commandSocketReference = IntPtr.Zero;
			
			if( socketType == SocketType.TCP )
			{
				socketReference = NeuronDataReader.BRConnectTo( address, port );
				if( socketReference != IntPtr.Zero )
				{
					Debug.Log( string.Format( "[NeuronConnection] Connected to {0}:{1}.", address, port ) );
				}
				else
				{
					Debug.LogError( string.Format( "[NeuronConnection] Connecting to {0}:{1} failed.", address, port ) );
				}
			}
			else
			{
				socketReference = NeuronDataReader.BRStartUDPServiceAt( port );
				if( socketReference != IntPtr.Zero )
				{
					Debug.Log( string.Format( "[NeuronConnection] Start listening at {0}.", port ) );
				}
				else
				{
					Debug.LogError( string.Format( "[NeuronConnection] Start listening at {0} failed.", port ) );
				}
			}
			
			if( socketReference != IntPtr.Zero )
			{
				if( connections.Count == 0 )
				{
					RegisterReaderCallbacks();
				}
				
				if( commandServerPort > 0 )
				{
					// connect to command server
					commandSocketReference = NeuronDataReader.BRConnectTo( address, commandServerPort );
					if( commandSocketReference != IntPtr.Zero )
					{
						Debug.Log( string.Format( "[NeuronConnection] Connected to command server {0}:{1}.", address, commandServerPort ) );
					}
					else
					{
						Debug.LogError( string.Format( "[NeuronConnection] Connected to command server {0}:{1} failed.", address, commandServerPort ) );
					}
				}
				
				source = new NeuronSource( address, port, commandServerPort, socketType, socketReference, commandSocketReference );
				connections.Add( source.guid, source );
				socketReferencesIndex.Add( socketReference, source );
				if( commandSocketReference != IntPtr.Zero )
				{
					commandSocketReferenceIndex.Add( commandSocketReference, source );
				}
			}
			
			return source;
		}
		
		static void DestroyConnection( NeuronSource source )
		{
			if( source != null )
			{
				if( source.commandSocketReference != IntPtr.Zero )
				{
					commandSocketReferenceIndex.Remove( source.commandSocketReference );
				}
			
				source.OnDestroy();
			
				Guid guid = source.guid;
				string address = source.address;
				int port = source.port;
				int commandServerPort = source.commandServerPort;
				SocketType socketType = source.socketType;
				IntPtr socketReference = source.socketReference;
				IntPtr commandSocketReference = source.commandSocketReference;
				
				connections.Remove( guid );
				socketReferencesIndex.Remove( socketReference );
				
				if( commandSocketReference != IntPtr.Zero )
				{
					NeuronDataReader.BRCloseSocket( commandSocketReference );
					Debug.Log( string.Format( "[NeuronConnection] Disconnected from command server {0}:{1}.", address, commandServerPort ) );
				}
				
				if( socketType == SocketType.TCP )
				{
					NeuronDataReader.BRCloseSocket( socketReference );
					Debug.Log( string.Format( "[NeuronConnection] Disconnected from {0}:{1}.", address, port ) );
				}
				else
				{
					NeuronDataReader.BRCloseSocket( socketReference );
					Debug.Log( string.Format( "[NeuronConnection] Stop listening at {0}. {1}", port, source.guid.ToString( "N" ) ) );
				}
			}
			
			if( connections.Count == 0 )
			{
				UnregisterReaderCallbacks();
			}
		}
		
		static void RegisterReaderCallbacks()
		{
			NeuronDataReader.BRRegisterFrameDataCallback( IntPtr.Zero, OnFrameDataReceived );
			NeuronDataReader.BRRegisterSocketStatusCallback( IntPtr.Zero, OnSocketStatusChanged );
		}
		
		static void UnregisterReaderCallbacks()
		{
			NeuronDataReader.BRRegisterFrameDataCallback( IntPtr.Zero, null );
			NeuronDataReader.BRRegisterSocketStatusCallback( IntPtr.Zero, null );
		}
		
		static void OnFrameDataReceived( IntPtr customObject, IntPtr socketReference, IntPtr header, IntPtr data )
		{
			NeuronSource source = FindSource( socketReference );
			if( source != null )
			{
				source.OnFrameDataReceived( header, data );
			}
		}
		
		static void OnSocketStatusChanged( IntPtr customObject, IntPtr socketReference, SocketStatus status, string msg )
		{
			NeuronSource source = FindSource( socketReference );
			if( source != null )
			{
				source.OnSocketStatusChanged( status, msg );
			}
		}
		
		static NeuronSource FindConnection( string address, int port, SocketType socketType )
		{
			NeuronSource source = null;
			foreach( KeyValuePair<Guid, NeuronSource> it in connections )
			{
				if( it.Value.socketType == SocketType.UDP && socketType == SocketType.UDP && it.Value.port == port )
				{
					source = it.Value;
					break;
				}
				else if( it.Value.socketType == SocketType.TCP && socketType == SocketType.TCP && it.Value.address == address && it.Value.port == port )
				{
					source = it.Value;
					break;
				}
			}
			return source;
		}
		
		static NeuronSource FindSource( IntPtr socketReference )
		{
			NeuronSource source = null;
			socketReferencesIndex.TryGetValue( socketReference, out source );
			return source;
		}
		
		static NeuronSource FindCommandSource( IntPtr commandSocketReference )
		{
			NeuronSource source = null;
			commandSocketReferenceIndex.TryGetValue( commandSocketReference, out source );
			return source;
		}
	}
}