using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RTS{
	public class Verticle {
		
		private MeshManager OwnerManager;
		private VerticleState state;
		public int number;//my own number of this alias
		private List<int> Aliases;	
		private List<int> Triangles;
		private List<int> LinkedAliases;//Aliases of verts with which this vert is making triangles
		public Vector3 positionRelative;
		public Vector3 positionAbsolute;
		
		
		public Verticle(MeshManager Owner ,int Xnumber, List<int> XAliases, Vector3 XpositionRelative){
			Initialise();
			OwnerManager = Owner;
			number = Xnumber;
			Aliases = XAliases;
			positionRelative = XpositionRelative;
			SetPositionAbsolute();
		}
		
		private void Initialise(){
			Triangles = new List<int>();
			LinkedAliases = new List<int>();
			Aliases = new List<int>();
			state = VerticleState.Standard;
			positionAbsolute = Vector3.zero;
		}
		
		private void SetPositionAbsolute(){
			positionAbsolute = 	OwnerManager.ParentPosition;
			positionAbsolute = positionAbsolute + positionRelative;
			
		}
		
		public void SetPositionRelative(Vector3 Pos){
			Pos = positionRelative;	
		}
		
		public void AddTriangle(int i, Vector3 vert){//vert is a vector with aliases of vector participating in triangle
			if(Triangles.Contains(i)){//if we arleady have it, there is no point in adding trangle number
				return;
			}
			Triangles.Add (i);
			
			CheckIfAddLinkedAlias((int)vert.x);
			CheckIfAddLinkedAlias((int)vert.y);
			CheckIfAddLinkedAlias((int)vert.z);
		}
		
		private void CheckIfAddLinkedAlias(int x){ 
			if((!Aliases.Contains(x)) && (!LinkedAliases.Contains(x)) ){
				LinkedAliases.Add(x);
			}
		}
		
		public void DestroyByHeight(float height){
			if(height<positionAbsolute.y){
				Destroy();
			}
		}
		
		public void DestroyByNumber(int numb){
			if(numb==number){
				Destroy();	
			}
		}
		
		private void Destroy(){
			foreach(int k in LinkedAliases){//send info to other aliases about breaking links
				OwnerManager.Aliases[k].RemoveLink(number);
				RemoveLink(k);
				foreach(int l in Triangles){
					OwnerManager.Aliases[k].RemoveTriangle(l);
				}
			}
			DestroyTriangles();

		}
				
		public void RemoveLink(int k){
			int index = LinkedAliases.IndexOf(k);
			LinkedAliases.Remove(index);
		}
		
		private void DestroyTriangles(){
			for(int i = 0 ; i<Triangles.Count; i++){
				int k = Triangles[i];
				OwnerManager.RemoveTriangle(k);
			}		
		}
		
		public void RemoveTriangle(int number){
			if(Triangles.Contains(number)){
				Triangles.Remove(number);	
			}
		}
}
	
}