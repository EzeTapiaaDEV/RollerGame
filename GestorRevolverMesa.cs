using UnityEngine;

public class GestorRevolverMesa : MonoBehaviour
{
    [Header("Referencias Principales")]
    public ControladorJugadorMovil jugadorMovil;
    public GameObject prefabRevolver;
    public Transform puntoSpawnMesa;

    private bool creandoRevolver = false;

    void Update()
    {
        if (creandoRevolver) return;

        if (jugadorMovil == null ||
            jugadorMovil.revolverPropio != null ||
            prefabRevolver == null ||
            puntoSpawnMesa == null)
        {
            return;
        }

        CrearRevolver();
    }

    private void CrearRevolver()
    {
        creandoRevolver = true;

        GameObject nuevoRevolver = Instantiate(
            prefabRevolver,
            puntoSpawnMesa.position,
            puntoSpawnMesa.rotation,
            puntoSpawnMesa
        );

        nuevoRevolver.transform.localPosition = Vector3.zero;
        nuevoRevolver.transform.localRotation = Quaternion.identity;
        nuevoRevolver.transform.localScale = Vector3.one;

        jugadorMovil.revolverPropio = nuevoRevolver;

        Rigidbody rb = nuevoRevolver.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;

        Debug.Log("Revólver creado correctamente en la mesa.");

        creandoRevolver = false;
    }
}
