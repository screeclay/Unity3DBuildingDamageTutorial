using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RTS;

public class MeshManager {//for each meshfilter there should be an distinct meshManager
	
	public Mesh mesh;
	private MeshFilter filter;
	public Vector3 ParentPosition;
	public List<Verticle> Aliases;
	public int[] VerticleToAliasArray;//the index is vert number, in its there is the number of its alias, its size is the number of veerticles
	public List<Vector3> Triangles;
	
	float MinimumHeight;
	float MaximumHeight;
	
	private VerticleState meshState;
	private float time = 0;
	public int OrgTriangleCount = 0;
	
	public bool IsMeshThick = false;
	
	
	
	public MeshManager (ref MeshFilter meshFilter){//constructor
		Initialise();
		ParentPosition = meshFilter.transform.position;
		filter = meshFilter;
		mesh = meshFilter.mesh;

		ManageVerticles(mesh.vertices);
		ManageTriangles(mesh.triangles);
		CalcualateMinimumHeight();
		MakeMeshThick();
		//AddTriangleAndVerticles(1,2,3);
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
	
	private void ManageVerticlesAddNewUpdateOld(Vector3[] verticles){
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
		int OrgAliasesCount = Aliases.Count;
		foreach(var pair in TempAliases){//a Key-Value Pair
			if(i>OrgAliasesCount-1){//we add a new verticle
				Aliases.Add (new Verticle(this, i, pair.Value, pair.Key));
			}else{
				Aliases[i].Modify(i, pair.Value, pair.Key);	
			}
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
	
	public Vector3 ChangeVerticleToAlias(Vector3 vect){
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
	
	public void TranslateFromMeshToManager(){
		Triangles = new List<Vector3>();
		ManageVerticlesAddNewUpdateOld(mesh.vertices);
		ManageTriangles(mesh.triangles);
		CalcualateMinimumHeight();
		
		if(IsMeshThick==true){
			for(int i=Aliases.Count/2; i<Aliases.Count; i++){
				Aliases[i].IsATwin = true;
			}
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
		Aliases[verticleNumber].StartFire();
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
		/*for(int i=0; i<Aliases.Count; i++){
			Aliases[i].DestroyByNumber(numb);
		}*/
		Aliases[numb].DestroyByNumber(numb);
		//UpdateTrianglesList();
		
	}
	
	public void TellAliasesToUpdate(){
		if(time>3){
			for(int k=0; k<Aliases.Count;k++){
				Aliases[k].Update();
			}
			time = 0;
		}else{
			time += Time.deltaTime;	
		}
	}
	
	public void MakeMeshThick(){
		OrgTriangleCount = (mesh.triangles.Length) /3;
		
		AddTwinsToVerticleArray();
		AddTwinsToTriangleAlias();
		
		TranslateFromMeshToManager();
		for(int i=Aliases.Count/2; i<Aliases.Count; i++){
			Aliases[i].IsATwin = true;
		}
		IsMeshThick = true;
	}
	
	private void AddTwinsToVerticleArray(){
		Vector3[] OldVerticleList = mesh.vertices;
		Vector3[] NewVerticleList = new Vector3[OldVerticleList.Length];
				
		float Wall_Thickness = 0.05f;
		Vector3 TargetPosition;
		Vector3 NewPosition ;

		for(int i=0; i<OldVerticleList.Length; i++){
			TargetPosition = mesh.vertices[i] - mesh.normals[i];
			NewPosition = Vector3.MoveTowards(mesh.vertices[i], TargetPosition, Wall_Thickness);
			NewVerticleList[i] = NewPosition;
		}
		
		Vector3[] final = new Vector3[OldVerticleList.Length + NewVerticleList.Length];
		OldVerticleList.CopyTo(final, 0);
		NewVerticleList.CopyTo(final, OldVerticleList.Length);
		
		mesh.vertices = final;
	}
	
	private void AddTwinsToTriangleAlias(){
		int[] OldTriangleList = mesh.triangles;
		int[] NewTriangleList = new int [OldTriangleList.Length];
		int offset = (mesh.vertices.Length/2); 
		for(int i=0; i<OldTriangleList.Length; i+=3){
			NewTriangleList[i] = OldTriangleList[ i + 2 ] + offset;
			NewTriangleList[i+1] = OldTriangleList[ i + 1 ] + offset;
			NewTriangleList[i+2] = OldTriangleList[ i ] + offset;
		}
		
		int[] final = new int[OldTriangleList.Length + NewTriangleList.Length];
		OldTriangleList.CopyTo(final, 0);
		NewTriangleList.CopyTo(final, OldTriangleList.Length);
		
		mesh.triangles = final;
	}
	
	public int AddTriangleAndVerticles(int a, int b, int c){//gets 3 numbers of Aliases. Produces a triangle between them, and new verticles
		Vector3[] OldVerticleList = mesh.vertices;
		Vector3[] NewVerticleList = new Vector3 [OldVerticleList.Length+3];
		Vector3 ktk = Aliases[0].positionRelative;
		Vector3[] LastThreeVerts = new Vector3[3]{Aliases[a].positionRelative,Aliases[b].positionRelative,Aliases[c].positionRelative  };
		OldVerticleList.CopyTo(NewVerticleList,0);
		LastThreeVerts.CopyTo(NewVerticleList, OldVerticleList.Length);
		mesh.vertices = NewVerticleList;
		
		
		int[] OldTriangleList = mesh.triangles;
		int[] NewTriangleList = new int [OldTriangleList.Length+3];
		int[] NewTriangle = new int[3]{mesh.vertices.Length-1, mesh.vertices.Length-2, mesh.vertices.Length-3};
		OldTriangleList.CopyTo(NewTriangleList, 0);
		NewTriangle.CopyTo(NewTriangleList, OldTriangleList.Length);
		mesh.triangles = NewTriangleList;
		return mesh.triangles.Length/3;  //the number of added triangle
	}
}
	
