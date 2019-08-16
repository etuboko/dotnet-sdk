// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Actions.Actors.Builder
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Actions.Actors.Description;

    internal class InterfaceDetailsStore
    {
        private const string TraceType = "InterfaceDetailsStore";

        private readonly ConcurrentDictionary<int, InterfaceDetails> knownTypesMap =
            new ConcurrentDictionary<int, InterfaceDetails>();

        private readonly ConcurrentDictionary<string, int> interfaceIdMapping =
            new ConcurrentDictionary<string, int>();

        public bool TryGetKnownTypes(int interfaceId, out InterfaceDetails interfaceDetails)
        {
            return this.knownTypesMap.TryGetValue(interfaceId, out interfaceDetails);
        }

        public bool TryGetKnownTypes(string interfaceName, out InterfaceDetails interfaceDetails)
        {
            if (!this.interfaceIdMapping.TryGetValue(interfaceName, out var interfaceId))
            {
                // TODO : Add EventSource diagnostics
                interfaceDetails = null;
                return false;
            }

            return this.knownTypesMap.TryGetValue(interfaceId, out interfaceDetails);
        }

        public void UpdateKnownTypeDetail(InterfaceDescription interfaceDescription, MethodBodyTypesBuildResult methodBodyTypesBuildResult)
        {
            var responseKnownTypes = new List<Type>();
            var requestKnownType = new List<Type>();
            foreach (var entry in interfaceDescription.Methods)
            {
                if (TypeUtility.IsTaskType(entry.ReturnType) && entry.ReturnType.GetTypeInfo().IsGenericType)
                {
                    var returnType = entry.MethodInfo.ReturnType.GetGenericArguments()[0];
                    if (!responseKnownTypes.Contains(returnType))
                    {
                        responseKnownTypes.Add(returnType);
                    }
                }

                requestKnownType.AddRange(entry.MethodInfo.GetParameters()
                    .ToList()
                    .Select(p => p.ParameterType)
                    .Except(requestKnownType));
            }

            var knownType = new InterfaceDetails();
            knownType.Id = interfaceDescription.Id;
            knownType.ServiceInterfaceType = interfaceDescription.InterfaceType;
            knownType.RequestKnownTypes = requestKnownType;
            knownType.ResponseKnownTypes = responseKnownTypes;
            knownType.MethodNames = interfaceDescription.Methods.ToDictionary(item => item.Name, item => item.Id);
            knownType.RequestWrappedKnownTypes = methodBodyTypesBuildResult.GetRequestBodyTypes();
            knownType.ResponseWrappedKnownTypes = methodBodyTypesBuildResult.GetResponseBodyTypes();
            this.UpdateKnownTypes(interfaceDescription.Id, interfaceDescription.InterfaceType.FullName, knownType);
        }

        private void UpdateKnownTypes(
            int interfaceId,
            string interfaceName,
            InterfaceDetails knownTypes)
        {
            if (this.knownTypesMap.ContainsKey(interfaceId))
            {
                // TODO : Add EventSource diagnostics
                return;
            }

            if (this.knownTypesMap.TryAdd(interfaceId, knownTypes))
            {
                this.interfaceIdMapping.TryAdd(interfaceName, interfaceId);
            }
        }
    }
}
