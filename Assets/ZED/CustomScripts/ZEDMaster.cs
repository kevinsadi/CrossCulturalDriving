#if USING_ZED
using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine.InputSystem;


public class ZEDMaster : Interactable_Object {
   
    private List<ZEDSkeletonAnimator> zedAvatars;
    private int currentAvatarIndex = 0;
    
    [SerializeField] private GameObject storedTarget;
    private ZEDSkeletonAnimator targetAnimator;
    private ZEDBodyTrackingManager zedBodyTrackingManager;
    private Transform targetHip;
    
    private Pose destinationPose;

    
    public NetworkVariable<NetworkObjectReference> SyncroizedHeadPositon;


    private void Start() {
        
    }

    public override void OnNetworkSpawn()
    {
        SyncroizedHeadPositon = new NetworkVariable<NetworkObjectReference>();
        base.OnNetworkSpawn();
        if (IsServer)
        {
            zedBodyTrackingManager =GetComponent<ZEDBodyTrackingManager>();
            destinationPose = new Pose(transform.position, transform.rotation);
        }
        else
        {
            GetComponent<ZEDBodyTrackingManager>().enabled = false;
            GetComponent<ZEDStreamingClient>().enabled = false;
        }


    }

    private void Update()
    {
        if (IsServer)
        {
            //HandleMouseClick();
            CycleThroughAvatars();
        }
    }

    private void OnDisable()
    {  if (IsServer)
        {
            // destroy all avatars
            ZEDSkeletonAnimator[] avatars = GameObject.FindObjectsOfType<ZEDSkeletonAnimator>();
            foreach (var avatar in avatars)
            {
                Destroy(avatar.gameObject);
            }
        }
    }

    private void CycleThroughAvatars()
    {
        if (IsServer)
        {
            // if press right arrow, go to next avatar
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                zedAvatars = new List<ZEDSkeletonAnimator>(FindObjectsOfType<ZEDSkeletonAnimator>());
                storedTarget = zedAvatars[currentAvatarIndex].gameObject;
                foreach (ZEDSkeletonAnimator zed in zedAvatars)
                {
                    SetMaterialsColor(zed.gameObject, Color.white);
                }

                SetMaterialsColor(storedTarget, Color.red);
                currentAvatarIndex++;
                if (currentAvatarIndex == zedAvatars.Count)
                {
                    currentAvatarIndex = 0;
                }
            }

            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                StartCoroutine(CalibrateSequence());
            }
        }
    }
    
    private IEnumerator CalibrateSequence()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            FindDependencies();
            CalibratePosition();
            yield return new WaitForSeconds(0.05f);
            CalibrateRotation();
            yield return new WaitForSeconds(0.05f);
            CalibratePosition();
            yield return new WaitForSeconds(0.05f);
            SyncroizedHeadPositon.Value = storedTarget.GetComponent<NetworkObjectReference>();
        }
    }

   
    
    private void FindDependencies()
    {
        // if there is only one avatar, assign it to storedTarget
        if (storedTarget == null)
        {
            ZEDSkeletonAnimator[] allAvatars = FindObjectsOfType<ZEDSkeletonAnimator>();
            if (allAvatars.Length == 1)
            {
                storedTarget = allAvatars[0].gameObject;
            }
            else
            {
                return;
            }
        }

        zedBodyTrackingManager = GetComponent<ZEDBodyTrackingManager>();
        
        // get the current hip position of target
        targetAnimator = storedTarget.GetComponent<ZEDSkeletonAnimator>();
        targetHip = targetAnimator.animator.GetBoneTransform(HumanBodyBones.Hips);

    }
    private void CalibratePosition()
    {
        // calculate the difference vector
        Vector3 positionOffset = destinationPose.position - targetHip.position;
        // apply difference
        zedBodyTrackingManager.positionOffset += positionOffset;
    }

    private void CalibrateRotation()
    {
        // calculate the difference vector
        Vector3 rotationOffset = destinationPose.rotation.eulerAngles - targetHip.rotation.eulerAngles;
        Debug.Log("rotationOffset: " + rotationOffset);
        // apply difference
        zedBodyTrackingManager.rotationOffset += new Vector3(0, rotationOffset.y, 0);
    }

    private void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            bool hasHit = Physics.Raycast(ray, out hit);
            GameObject hitObject = hasHit ? hit.collider.gameObject : null;

            if (storedTarget != null && (hitObject == null || hitObject != storedTarget))
            {
                SetMaterialsColor(storedTarget, Color.white);
                storedTarget = null; 
            }

            if (hitObject && hitObject.GetComponentInParent<ZEDSkeletonAnimator>())
            {
                GameObject currentTarget = hitObject.GetComponentInParent<ZEDSkeletonAnimator>().gameObject;
                SetMaterialsColor(currentTarget, Color.red);
                storedTarget = currentTarget;
                StartCoroutine(CalibrateSequence());
            }
        }
    }
    
    public void SetMaterialsColor(GameObject target, Color color)
    {
        SkinnedMeshRenderer skinnedMeshRenderer = target.GetComponentInChildren<SkinnedMeshRenderer>();
        Material[] mats = skinnedMeshRenderer.materials;
        foreach (Material mat in mats)
        {
            mat.color = color;
        }
        skinnedMeshRenderer.materials = mats;
    }

    public override void Stop_Action()
    {
    }

    private ParticipantOrder _participantOrder;
    private ulong CLID_;
    public override void AssignClient(ulong _CLID_, ParticipantOrder _participantOrder_)
    {
        _participantOrder = _participantOrder_;
        CLID_= _CLID_;
    }

    public override Transform GetCameraPositionObject()
    {
        Debug.Log("Attempting to retrive  headposition for client in ZedMaster ");
        if (SyncroizedHeadPositon.Value.TryGet(out NetworkObject no))
        {
            
            Transform headContainer = no.transform.GetComponentInChildren<HeadContainer>().transform;
            Debug.Log($"Found object on Client {headContainer.name}");
            float hipRotY = storedTarget.GetComponent<ZEDSkeletonAnimator>().animator
                .GetBoneTransform(HumanBodyBones.Hips).eulerAngles.y;
            Client_Object obj= null;
            
            foreach (var cod in FindObjectsOfType<Client_Object>())
            {
                if (cod.OwnerClientId == CLID_)
                {
                    obj = cod;
                    break;
                }
            }

            if (obj == null)
            {
                return  null;
            }
            
            float camRotY = obj.GetMainCamera().transform.localRotation.eulerAngles.y;
            float rotationDifferenceY = hipRotY - camRotY;
            Vector3 containerRotation = new Vector3(0, rotationDifferenceY, 0);
            headContainer.localRotation = Quaternion.Euler(containerRotation);
            return headContainer;
        }
        else
        {
            Debug.Log("Could not resolve the head transform object!");
        }

        return null;
    }

    public override void SetStartingPose(Pose _pose)
    {
       
    }

    public override bool HasActionStopped()
    {
        return true;
    }
}

#endif