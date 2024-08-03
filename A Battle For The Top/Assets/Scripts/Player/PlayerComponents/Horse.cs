using BFTT;
using BFTT.Abilities;
using BFTT.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Horse : MonoBehaviour
{

    public Transform saddlePosition;
    public Transform leftHandPosition;
    public Transform rightHandPosition;
    public Transform leftFootPosition;
    public Transform rightFootPosition;

    public RigidbodyMover _mover;

    [HideInInspector] public bool _beingRode;

    public AbilityScheduler _scheduler;


    bool happenOnce = false;

    // Start is called before the first frame update
    void Start()
    {
        _mover.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(_beingRode)
        {
            _mover.Move(_scheduler.characterActions.move, 8f);
            Debug.Log(_scheduler.characterActions.move);
            happenOnce = false;
        }
        else
        {
            if (!happenOnce)
            {
                _mover.enabled = false;
                happenOnce = true;
            }
        }
    }
}
