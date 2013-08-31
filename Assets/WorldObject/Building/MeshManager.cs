using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RTS;

public class MeshManager {//for each meshfilter there should be an distinct meshManager
	
	public Mesh mesh;
	public Vector3 ParentPosition;
	public List<Verticle> Aliases;
	public int[] VerticleToAliasArray;//the index is vert number, in its there is the number of its alias, its size is the number of veerticles
	public List<Vector3> Triangles;
	
	float MinimumHeight;
	float MaximumHeight;
	
	private VerticleState meshState;
	private float time = 0;
	float speed = 100f;
	
	private bool DoWeMakeDoubleWalls = false;
	
	
	
	public MeshManager (ref MeshFilter meshFilter){//constructor
		Initialise();
		ParentPosition = meshFilter.transform.position;
		mesh = meshFilter.mesh;
		ManageVerticles(mesh.vertices);
		ManageTriangles(mesh.triangles);
		CalcualateMinimumHeight();
	}
	

	
	private void Initialise () {
		Aliases = new List<Verticle>();
		Triangles = new List<Vector3>();
		meshState = VerticleState.Standard;
	}
	
	private void ManageVerticles(Vector3[] verticles){//fnction find where many verticles are in the same positions and in that way produces its aliases
		Dictionary<Vector3, List<int>> TempAliases = new Dictionary<Vector3, List<int>>(); //list of ints are numbers of verticles in the specific position (the Vector3)
		
		int i = 0;
		foreach (Vector3 verticle in verticles){
			if(TempAliases.ContainsKey(verticle)){//we've arleady added a verticle of this position, so the alias will have multiple verticles
				TempAliases[verticle].Add(i);
			}else{//we have to add a number of our verticle to our list
				TempAliases[verticle] = new List<int>();
				TempAliases[verticle].Add(i);
			}
			i++;	
		}
		VerticleToAliasArray = new int[i];
		
		i = 0;
		foreach(var pair in TempAliases){//a Key-Value Pair
			Aliases.Add (new Verticle(this, i, pair.Value, pair.Key));
			ManageVerticleToAliasArray(i,pair.Value);
			i++;
		}
	}
	
	private void ManageVerticleToAliasArray(int i, List<int> list){//we are trying to fill the Verticle to Alias Array
		foreach(int k in list){
			VerticleToAliasArray[k] = i;	
		}
	}
	
	private void ManageTriangles(int[] triangles){//info about triangles is stored in an array. blblblba
		for(int i=0; i<triangles.Length; i+=3){
			Triangles.Add(new Vector3(triangles[i], triangles[i+1], triangles[i+2]));//ok, we have a nice list of triangles, what now?
		}
		
		int j = 0;
		foreach(Vector3 vect in Triangles){//we pass info to verticle bout the triangles it particip in
			Aliases[VerticleToAliasArray[(int)vect.x]].AddTriangle(j,ChangeVerticleToAlias(vect));
			Aliases[VerticleToAliasArray[(int)vect.y]].AddTriangle(j,ChangeVerticleToAlias(vect));
			Aliases[VerticleToAliasArray[(int)vect.z]].AddTriangle(j,ChangeVerticleToAlias(vect));
			j++;
		}
	}
	
	private Vector3 ChangeVerticleToAlias(Vector3 vect){
		Vector3 x = new Vector3(VerticleToAliasArray[(int)vect.x], VerticleToAliasArray[(int)vect.y], VerticleToAliasArray[(int)vect.z]);
		return x;
	}
	
	public void CalcualateMinimumHeight(){//Counting absolute minimum and maximum height of verticle in the mesh
		Vector3[] verticles = mesh.vertices;	
		foreach(Verticle vert in Aliases){
			if(vert.positionAbsolute.y < MinimumHeight){ MinimumHeight = vert.positionAbsolute.y;}
			if(vert.positionAbsolute.y > MaximumHeight){ MaximumHeight = vert.positionAbsolute.y;}
		}
	}
	
	public void Destroy (float percent){ 
		float DestroyHeight	= MaximumHeight -(MaximumHeight - MinimumHeight)*percent;// ParentPosition.y + 0.66f;
		foreach(Verticle vert in Aliases){
			vert.DestroyByHeight(DestroyHeight);
		}
		UpdateTrianglesList();
	}
	
	public void StartFire(int verticleNumber){

		MakeMeshThick();
		
	}
	
	public void RemoveTriangle(int k){
		Vector3 triangleNumbers = Triangles[k];
		Vector3 aliasNumbers = ChangeVerticleToAlias(triangleNumbers);

		Triangles[k] = Vector3.zero;
	}
	
	public void UpdateTrianglesList(){//gets the in-manager triangle list and then translates it to mesh.triangles format, and then sets it to be the mesh.triangles
		
		int LenghtOfTrianglesArray = Triangles.Count*3;
		int[] tempTrianglesArray = new int[LenghtOfTrianglesArray];
		int i = 0;
		foreach(Vector3 vec in Triangles){
			tempTrianglesArray[i] 	= (int)vec.x;
			tempTrianglesArray[i+1] = (int)vec.y;
			tempTrianglesArray[i+2] = (int)vec.z;
			i+=3;
		}
		mesh.triangles = tempTrianglesArray;
	}
	
	public void UpdateVerticlesList(){
		
		Vector3[] temp = new Vector3[VerticleToAliasArray.Length];
		
		for(int i=0; i< VerticleToAliasArray.Length; i++){
			int k = VerticleToAliasArray[i];
			temp[i] = Aliases[k].positionRelative;
		}
		mesh.vertices = temp;		
		
	}
	
	public void DestroyByNumber(int numb){
		foreach(Verticle vert in Aliases){
			vert.DestroyByNumber(numb);
		}
		UpdateTrianglesList();
		
	}
	
	public void TellAliasesToUpdate(){
		if(time>1){
			for(int k=0; k<Aliases.Count;k++){
				Aliases[k].Update();
			}
			time = 0;
		}else{
			time += Time.deltaTime;	
		}
	}
	
	public void MakeMeshThick(){
		List<Verticle> NewAliases = MakeAliasesTwin(Aliases);
		List<Vector3> NewTriangles = InvertTrianglesGivingListOfTriangles(Triangles);
		NewTriangles = AddOffsetToTriangleList(NewTriangles, mesh.vertices.Length);
		AddTwinsToVerticleToAliasArray();
		
		for(int i=0; i<Aliases.Count; i++){//lets update the Aliases list by Adding new Aliases
			Aliases[i].DoHaveTwin = true;//sending info to Alias that it have a twin, Numbers of twins is the number of original aliases
		}
		
		Aliases.AddRange(NewAliases);
		Triangles.AddRange(NewTriangles);

		UpdateVerticlesList();
		UpdateTrianglesList();
	}
	
	
	private List<Verticle> MakeAliasesTwin(List<Verticle> XAliases){
		float Wall_Thickness = 0.005f;
		Vector3 TargetPosition;
		Vector3 NewPosition ;
		int offset = Aliases.Count;
		List<Verticle> NewAliases = new List<Verticle>();// XAliases;
		
		for(int i=0; i<XAliases.Count; i++){
			TargetPosition = XAliases[i].positionRelative-(XAliases[i].normal);
			NewPosition = Vector3.MoveTowards(XAliases[i].positionRelative, TargetPosition, Wall_Thickness);
			NewAliases.Add(new Verticle(this,XAliases[i].number+offset,XAliases[i].Aliases,NewPosition) );//copy an alias
			NewAliases[i].AddOffsetToAliases(mesh.vertices.Length);
			
		}	
		return NewAliases;
		
	} 
	
	private void InvertTriangles(Mesh givenMesh = null){
		if(givenMesh == null){givenMesh = mesh;}
		givenMesh.triangles = givenMesh.triangles.Reverse().ToArray();

	}
	
	private List<Vector3> InvertTrianglesGivingListOfTriangles(List<Vector3> XTriangles = null){
		if(XTriangles == null){XTriangles = Triangles;}
		List<Vector3> temp = new List<Vector3>();
		for(int i=0;i<XTriangles.Count;i++){
			temp.Add( new Vector3(XTriangles[  i].z,XTriangles[  i].y,XTriangles[ i].x ) );
		}
		return temp;
	}	
			
	private List<Vector3> AddOffsetToTriangleList(List<Vector3> XTriangles, int offset){
		List<Vector3> Temp = new List<Vector3>();
		for(int i=0; i<XTriangles.Count; i++){
			Temp.Add(new Vector3(XTriangles[i].x+offset, XTriangles[i].y+offset, XTriangles[i].z+offset));
		}
		return Temp;
	}
	
	private void AddTwinsToVerticleToAliasArray(){

		int[] NewVTAA = new int[VerticleToAliasArray.Length];
		int offset = Aliases.Count;
		for(int i=0; i<VerticleToAliasArray.Length; i++){
				NewVTAA[i] = VerticleToAliasArray[i] + offset;
		}
		int[] z = new int[VerticleToAliasArray.Length + NewVTAA.Length];
		VerticleToAliasArray.CopyTo(z, 0);
		NewVTAA.CopyTo(z, VerticleToAliasArray.Length);
		VerticleToAliasArray = z;
	}
	
}
	
