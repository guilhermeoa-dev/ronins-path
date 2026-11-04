using UnityEngine;
// Aula 03x03: Start, mover(), PingPong e parentesco no contato com Player.
public class Plataforma : MonoBehaviour
{
    public float Distancia = 5, VelocidadeMovimento = 1.5f;
    public Vector2 Direcao = Vector2.right;
    public bool ativada;
    Transform TransformPlataforma;
    Vector3 PosicaoInicial;
    float TempoDecorrido;
    void Start()
    {
        TransformPlataforma = GetComponent<Transform>();
        PosicaoInicial = TransformPlataforma.position;
    }

    void Update()
    {
        if (ativada)
            mover();
    }

    void mover()
    {
        TempoDecorrido += Time.deltaTime * VelocidadeMovimento;
        TransformPlataforma.position = PosicaoInicial + (Vector3) Direcao.normalized * Mathf.PingPong(TempoDecorrido, Distancia);
    }

    void OnCollisionEnter2D(Collision2D objeto)
    {
        if (objeto.gameObject.CompareTag("Player") && objeto.transform.position.y > transform.position.y)
            objeto.transform.SetParent(TransformPlataforma, true);
    }

    void OnCollisionExit2D(Collision2D objeto)
    {
        if (objeto.gameObject.CompareTag("Player") && objeto.transform.parent == TransformPlataforma)
            objeto.transform.SetParent(null, true);
    }
}
