﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;

namespace Microsoft.Coyote.SmartSockets
{
    /// <summary>
    /// A special DataContractResolver used by SmartSocket class to resolve serialized types.
    /// </summary>
    public class SmartSocketTypeResolver : DataContractResolver
    {
        private readonly Dictionary<string, Type> TypeMap = new Dictionary<string, Type>();

        /// <summary>
        /// Construct new SmartSocketTypeResolver object, with the SocketMessage type automatically registered.
        /// </summary>
        public SmartSocketTypeResolver()
        {
            this.AddBaseTypes();
        }

        /// <summary>
        /// Construct new SmartSocketTypeResolver object with the given set of known serializable types.
        /// </summary>
        /// <param name="knownTypes">The list of types to register</param>
        public SmartSocketTypeResolver(params Type[] knownTypes)
        {
            this.AddTypes(knownTypes);
        }

        /// <summary>
        /// Construct new SmartSocketTypeResolver object with the given set of known serializable types.
        /// </summary>
        /// <param name="knownTypes">The list of types to register</param>
        public SmartSocketTypeResolver(IEnumerable<Type> knownTypes)
        {
            this.AddTypes(knownTypes);
        }

        private void AddTypes(IEnumerable<Type> knownTypes)
        {
            this.AddBaseTypes();
            foreach (var t in knownTypes)
            {
                this.TypeMap[t.FullName] = t;
            }
        }

        private void AddBaseTypes()
        {
            foreach (var t in new Type[] { typeof(SocketMessage) })
            {
                this.TypeMap[t.FullName] = t;
            }
        }

        /// <summary>
        /// Implementation of DataContractResolver method
        /// </summary>
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            string fullName = typeName;
            if (!string.IsNullOrEmpty(typeNamespace))
            {
                Uri uri = new Uri(typeNamespace);
                string clrNamespace = uri.Segments.Last();
                fullName = clrNamespace + "." + typeName;
            }

            if (!this.TypeMap.TryGetValue(fullName, out Type t))
            {
                t = knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, knownTypeResolver);
            }

            return t;
        }

        /// <summary>
        /// Implementation of DataContractResolver method
        /// </summary>
        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            return knownTypeResolver.TryResolveType(type, declaredType, knownTypeResolver, out typeName, out typeNamespace);
        }
    }
}
