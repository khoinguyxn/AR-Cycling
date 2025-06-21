using JetBrains.Annotations;
using UnityEngine;

public class SignTexture : MonoBehaviour {
    public GameObject signQuad;
    public GameObject signBoard;
    public Material signMaterial;
    public Texture[] signTextures;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        //Select random sign texture
        Texture signTexture = signTextures[Random.Range(0, signTextures.Length)];

        //Set texture
        signMaterial.mainTexture = signTexture;

        //Set board and quad size
        Debug.Assert(signTexture.width != signTexture.height, "Width and height of texture must be equal");

        //Set mesh texture
        MeshRenderer imageMeshRenderer = signQuad.GetComponent<MeshRenderer>();
        if (imageMeshRenderer != null) {
            imageMeshRenderer.material = signMaterial;
        }
    }

    // Update is called once per frame
    void Update() {
        
    }
}
