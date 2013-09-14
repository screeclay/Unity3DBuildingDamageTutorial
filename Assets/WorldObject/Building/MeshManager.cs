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
	public List<int> WallsWaitingToBeMade;
	
	float MinimumHeight;
	float MaximumHeight;
	
	private VerticleState meshState;
	private float time = 0;
	public int OrgTriangleCount = 0;
	
	public bool IsMeshThick = false;
	
	public float DebDestroyTime = 0f;
	public float DebGeneralDestroyTime = 0f;
	public int DebDestroyCount = 0;
	public int DebGeneralNumber = 0;
	
	public bool MeshWasChanged = false;
	private int[] TempMeshTrinaglesArray;
	private Vector3[] TempMeshVerticlesArray;
	private int NumberOfGroundAliases = 0;
	
	public MeshManager (ref MeshFilter meshFilter){//constructor
		Initialise();
		ParentPosition = meshFilter.transform.position;
		filter = meshFilter;
		mesh = meshFilter.mesh;
		ManageVerticles(mesh.vertices);
		ManageTriangles(mesh.triangles);
		CalcualateMinimumHeight();
		CalculateNormals();
		MakeMeshThick();		
		//SetGroundAliases(0.5f);
		//CheckIfSeparated();
	}
	

	
	private void Initialise () {
		Aliases = new List<Verticle>();
		Triangles = new List<Vector3>();
		meshState = VerticleState.Standard;
		WallsWaitingToBeMade = new List<int>();
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
	
	public void CalculateNormals(){//Counting absolute minimum and maximum height of verticle in the mesh
		foreach(Verticle vec in Aliases){
			vec.CalculateNormal();	
		}
	}
	
	public void TranslateFromMeshToManager(){
		Triangles = new List<Vector3>(); ;
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
		

	}
	
	public void FFire(int verticleNumber){
		Aliases[verticleNumber].StartFire();
	}
	
	public void RemoveTriangle(int k){

		Triangles[k] = Vector3.zero;
	}
	
	public void UpdateTrianglesList(){//gets the in-manager triangle list and then translates it to mesh.triangles format, and then sets it to be the mesh.triangles. If mesh.traingles is longer than Traingles*3, writes from 0 to traingles*3, rest of mesh.traingles is left unchanged
		//Debug.Log("UPL");
		int LenghtOfTrianglesArray = Triangles.Count*3;
		int[] tempTrianglesArray = new int[LenghtOfTrianglesArray];
		int i = 0;
		foreach(Vector3 vec in Triangles){
			tempTrianglesArray[i] 	= (int)vec.x;
			tempTrianglesArray[i+1] = (int)vec.y;
			tempTrianglesArray[i+2] = (int)vec.z;
			i+=3;
		}
		int[] tempTrianglesArrayNumber2 = new int[mesh.triangles.Length];
		mesh.triangles.CopyTo(tempTrianglesArrayNumber2,0);
		tempTrianglesArray.CopyTo(tempTrianglesArrayNumber2, 0);

		mesh.triangles = tempTrianglesArrayNumber2;
		//mesh.triangles = tempTrianglesArray;
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
		Aliases[numb].DestroyByNumber(numb);	
			MakeWallsFromList();
			UpdateTrianglesList();
			TranslateFromMeshToManager();
	}
	
	public void FireAliasesBySphere(Vector3 pos){
		/*float SphereRadius = 0.5f;
		foreach(Verticle ver in Aliases){
			if(Vector3.Distance(ver.positionRelative, pos)<SphereRadius){
				ver.StartFire();	
			}
		}*/
	}
	
	public void TellAliasesToUpdate(){	
		
		DebDestroyCount = 0;
		DebDestroyTime = 0;
		
		
		if(time>1){
			for(int k=0; k<Aliases.Count;k++){
				Aliases[k].Update();
			}
			time = 0;
		}else{
			time += Time.deltaTime;	
		}
		

		if(MeshWasChanged){
			
			MakeWallsFromList();
			UpdateTrianglesList();
			TranslateFromMeshToManager();
			MeshWasChanged = false;
		}
		
		if(DebDestroyCount>0){
			DebGeneralNumber++;
			DebGeneralDestroyTime += DebDestroyTime;
			
		}
	}
	
	
	public void MakeMeshThick(){
		OrgTriangleCount = (mesh.triangles.Length) /3;
		AddTwinsToVerticleArray();
		AddTwinsToTriangleAlias();
		AddTwinsToUvArray();
		
		TranslateFromMeshToManager();
		for(int i=Aliases.Count/2; i<Aliases.Count; i++){
			Aliases[i].IsATwin = true;
		}
		IsMeshThick = true;
	}
	
	private void AddTwinsToVerticleArray(){
		Vector3[] OldVerticleList = mesh.vertices;
		Vector3[] NewVerticleList = new Vector3[OldVerticleList.Length];
				
		float Wall_Thickness = 0.005f;
		Vector3 TargetPosition;
		Vector3 NewPosition ;

		for(int i=0; i<OldVerticleList.Length; i++){
			
			TargetPosition = mesh.vertices[i] - Aliases[VerticleToAliasArray[i]].normal;
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
	
	private void AddTwinsToUvArray(){
		Vector2[] OldUvList = mesh.uv;//what is important here is that there arleady is automaticly-generated uv. We have to ovverride them.
		Vector2[] TwinUv = new Vector2[OldUvList.Length/2];
		//int offset = OldUvList.Length/2;
		for(int i=0; i<OldUvList.Length/2; i++){
			TwinUv[i] = OldUvList[i];	
		}
		TwinUv.CopyTo(OldUvList,OldUvList.Length/2);
		mesh.uv = OldUvList;
	}
	
	public int AddTriangleAndVerticles(int a, int b, int c){//gets 3 numbers of Aliases. Produces a triangle between them, and new verticles
		Vector3[] OldVerticleList = TempMeshVerticlesArray;
		Vector3[] NewVerticleList = new Vector3 [TempMeshVerticlesArray.Length+3];
		Vector3[] LastThreeVerts = new Vector3[3]{Aliases[a].positionRelative,Aliases[b].positionRelative,Aliases[c].positionRelative  };
		OldVerticleList.CopyTo(NewVerticleList,0);
		LastThreeVerts.CopyTo(NewVerticleList, TempMeshVerticlesArray.Length);
		TempMeshVerticlesArray = NewVerticleList;
		
		
		int[] OldTriangleList = TempMeshTrinaglesArray;
		int[] NewTriangleList = new int [TempMeshTrinaglesArray.Length+3];
		int[] NewTriangle = new int[3]{TempMeshVerticlesArray.Length-1, TempMeshVerticlesArray.Length-2, TempMeshVerticlesArray.Length-3};
		OldTriangleList.CopyTo(NewTriangleList, 0);
		NewTriangle.CopyTo(NewTriangleList, OldTriangleList.Length);
		TempMeshTrinaglesArray = NewTriangleList;
		return (TempMeshTrinaglesArray.Length/3)-1;  //the number of added triangle
	}
	
	public void AddWallToWallsList(int a, int b, int c){
		WallsWaitingToBeMade.Add(a);
		WallsWaitingToBeMade.Add(b);
		WallsWaitingToBeMade.Add(c);
	}
	
	private void MakeWallsFromList(){//this check if verticles of teoretical Alias are still Ok. If so, sends info to make trinagle from them.
		TempMeshVerticlesArray = new Vector3[mesh.vertices.Length];
		mesh.vertices.CopyTo(TempMeshVerticlesArray,0);
		
		TempMeshTrinaglesArray = new int[mesh.triangles.Length];
		mesh.triangles.CopyTo(TempMeshTrinaglesArray,0);
		
		float time = Time.realtimeSinceStartup;
		for(int i=0; i<WallsWaitingToBeMade.Count; i+=3){
			bool ItIsOk = true;
			
			if(WallsWaitingToBeMade[i]<Aliases.Count/2 && (Aliases[WallsWaitingToBeMade[i]].state != VerticleState.Destroyed)){
			//well, do nothin ,its ok
			}else{
				if(WallsWaitingToBeMade[i]>=Aliases.Count/2){//it is a Twin!, so dont have state
					if(	Aliases[WallsWaitingToBeMade[i]-Aliases.Count/2].state != VerticleState.Destroyed ){//check the Alias
					//well, do nothin	
					}else{ItIsOk = false;}
				}else{ItIsOk = false;}
			}
			
			if(WallsWaitingToBeMade[i+1]<Aliases.Count/2 && (Aliases[WallsWaitingToBeMade[i+1]].state != VerticleState.Destroyed)){
			//well, do nothin
			}else{
				if(WallsWaitingToBeMade[i+1]>=Aliases.Count/2){//it is a Twin!, so dont have state
					if(	Aliases[WallsWaitingToBeMade[i+1]-Aliases.Count/2].state != VerticleState.Destroyed ){//check the Alias
					//well, do nothin	
					}else{ItIsOk = false;}
				}else{ItIsOk = false;}
			}
			
			if(WallsWaitingToBeMade[i+2]<Aliases.Count/2 && (Aliases[WallsWaitingToBeMade[i+2]].state != VerticleState.Destroyed)){
			//well, do nothin
			}else{
				if(WallsWaitingToBeMade[i+2]>=Aliases.Count/2){//it is a Twin!, so dont have state
					if(	Aliases[WallsWaitingToBeMade[i+2]-Aliases.Count/2].state != VerticleState.Destroyed ){//check the Alias
					//well, do nothin	
					}else{ItIsOk = false;}
				}else{ItIsOk = false;}
			}
			
			if(ItIsOk == true){
				AddTriangleAndVerticles(	WallsWaitingToBeMade[i], WallsWaitingToBeMade[i+1], WallsWaitingToBeMade[i+2]);	
			}
		}
			
		
		WallsWaitingToBeMade.Clear();
			mesh.vertices = TempMeshVerticlesArray;
			mesh.triangles = TempMeshTrinaglesArray;
		DebDestroyTime += (Time.realtimeSinceStartup - time);
	}
	
	private void SetGroundAliases(float percent){
		float CuttingPosition = MaximumHeight - (MaximumHeight-MinimumHeight)*percent;
		foreach(Verticle v in Aliases){
			if(v.positionAbsolute.y<CuttingPosition){
				v.state = VerticleState.Ground;
				NumberOfGroundAliases++;
			}
		}

	}
	
	private void CheckIfSeparated(){
		int currentNumber = -1;
		for(int i=0; i<Aliases.Count/2; i++){
			VerticleState state = Aliases[i].state;
			if((state == VerticleState.Standard)||(state == VerticleState.Burning)){//we have found a normal Alias
				currentNumber = i;
				break;
			}
		}
		
		if(currentNumber != -1){  //if we in fact found something
			List<int> LinkedList = new List<int>();
			int CurrentNumberOfGroundAliases = 0;
			SeparationCheckLinkedAliases(currentNumber, LinkedList, ref CurrentNumberOfGroundAliases);
			
		}
		
		
	}
	
	private void SeparationCheckLinkedAliases(int currentNumber, List<int> LinkedList, ref int CurrentNumberOfGroundAliases){
		if(!LinkedList.Contains(currentNumber)){
			LinkedList.Add(currentNumber);
			List<int> LinkedAliases = Aliases[currentNumber].LinkedAliases;
			if(Aliases[currentNumber].state==VerticleState.Ground){
				CurrentNumberOfGroundAliases++;	
			}
			foreach(int i in LinkedAliases){
				if(!LinkedList.Contains(i)){
					SeparationCheckLinkedAliases(i, LinkedList,ref CurrentNumberOfGroundAliases);	
				}
			}
		}
		
	}
}
	
