using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LocalConstraints : MonoBehaviour {
    public bool FreezeX;
    public bool FreezeY;
    public bool FreezeZ;

    private Rigidbody rbody;
    private Vector3 localpos;

    public void Start() {
        rbody = GetComponent<Rigidbody>();
        localpos = transform.localPosition;
    }

    public void FixedUpdate() {
        Vector3 position = transform.localPosition;
        Vector3 velocity = transform.parent.InverseTransformVector(rbody.velocity);
        if (FreezeX) {
            position.x = localpos.x;
            velocity.x = 0;
        }
        if (FreezeY) {
            position.y = localpos.y;
            velocity.y = 0;
        }
        if (FreezeZ) {
            position.z = localpos.z;
            velocity.z = 0;
        }

        transform.localPosition = localpos;
        rbody.velocity = transform.parent.TransformVector(velocity);

        localpos = transform.localPosition;
    }
}
