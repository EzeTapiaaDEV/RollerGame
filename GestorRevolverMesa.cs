using UnityEngine;

public class GestorRevolverMesa : MonoBehaviour
{
    [Header("Referencias Principales")]
    public ControladorJugadorMovil jugadorMovil; // Arrastra aquí el objeto o script de tu jugador
    public GameObject prefabRevolver;             // Arrastra aquí el Prefab del revólver
    public Transform puntoSpawnMesa;              // El transform vacío en la mesa donde aparecerá

    void Update()
    {
        // Validamos que las referencias existan y que el revólver del jugador sea nulo (se haya destruido)
        if (jugadorMovil != null && jugadorMovil.revolverPropio == null && prefabRevolver != null && puntoSpawnMesa != null)
        {
            // Nos aseguramos de que no haya un revólver activo en la mano antes de crear uno nuevo en la mesa
            if (jugadorMovil.manoSocket != null)
            {
                foreach (Transform hijo in jugadorMovil.manoSocket)
                {
                    if (hijo.name.Contains("Revolver")) 
                    {
                        return; // Si todavía está en la mano, no hacemos nada todavía
                    }
                }
            }

            // Instanciamos el revólver nuevo de forma limpia en la mesa
            GameObject nuevoRev = Instantiate(prefabRevolver, puntoSpawnMesa.position, puntoSpawnMesa.rotation);
            nuevoRev.transform.SetParent(puntoSpawnMesa);
            nuevoRev.transform.localPosition = Vector3.zero;
            nuevoRev.transform.localRotation = Quaternion.identity;
            nuevoRev.transform.localScale = Vector3.one;

            // Le asignamos la nueva referencia al script principal del jugador
            jugadorMovil.revolverPropio = nuevoRev;
            
            Debug.Log("Revólver recreado exitosamente en la mesa por el gestor separado.");
        }
    }
}