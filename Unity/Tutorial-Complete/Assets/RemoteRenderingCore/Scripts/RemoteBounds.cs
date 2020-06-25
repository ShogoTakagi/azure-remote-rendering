﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Azure.RemoteRendering;
using Microsoft.Azure.RemoteRendering.Unity;
using System;
using UnityEngine;

[RequireComponent(typeof(BaseRemoteRenderedModel))]
public class RemoteBounds : BaseRemoteBounds
{
    //Remote bounds works with a specific remotely rendered model
    private BaseRemoteRenderedModel targetModel = null;

    private BoundsQueryAsync remoteBoundsQuery = null;

    private RemoteBoundsState currentBoundsState = RemoteBoundsState.NotReady;

    public override RemoteBoundsState CurrentBoundsState
    {
        get => currentBoundsState;
        protected set
        {
            if (currentBoundsState != value)
            {
                currentBoundsState = value;
                BoundsStateChange?.Invoke(value);
            }
        }
    }

    public override event Action<RemoteBoundsState> BoundsStateChange;

    public void Awake()
    {
        BoundsStateChange += HandleUnityEvents;
        targetModel = GetComponent<BaseRemoteRenderedModel>();

        targetModel.ModelStateChange += TargetModel_OnModelStateChange;
        TargetModel_OnModelStateChange(targetModel.CurrentModelState);
    }

    private void TargetModel_OnModelStateChange(ModelState state)
    {
        switch (state)
        {
            case ModelState.Loaded:
                QueryBounds();
                break;
            default:
                BoundsBoxCollider.enabled = false;
                CurrentBoundsState = RemoteBoundsState.NotReady;
                break;
        }
    }

    // Create a query using the model entity
    private void QueryBounds()
    {
        remoteBoundsQuery = targetModel.ModelEntity.QueryLocalBoundsAsync();
        CurrentBoundsState = RemoteBoundsState.Updating;
        remoteBoundsQuery.Completed += ProcessQueryResult;
    }

    // Check the result and apply it to the local Unity bounding box if it was successful
    private void ProcessQueryResult(BoundsQueryAsync remoteBounds)
    {
        if (remoteBounds.IsRanToCompletion)
        {
            var newBounds = remoteBounds.Result.toUnity();
            BoundsBoxCollider.center = newBounds.center;
            BoundsBoxCollider.size = newBounds.size;
            BoundsBoxCollider.enabled = true;
            CurrentBoundsState = RemoteBoundsState.Ready;
        }
        else
        {
            CurrentBoundsState = RemoteBoundsState.Error;
        }
    }
}