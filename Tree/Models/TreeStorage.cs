﻿using Tree.Contracts;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tree.Models
{
    public class TreeStorage
    {
        private IMongoDatabase _database;

        private IMongoCollection<TreeEntity> _treesCollections;
        private IMongoCollection<NodeEntity> _nodesCollections;

        private const string TREES_COLLECTION_NAME = "trees";
        private const string NODES_COLLECTION_NAME = "nodes";

        public TreeStorage(IMongoDatabase database)
        {
            _database = database;
            _treesCollections = _database.GetCollection<TreeEntity>(TREES_COLLECTION_NAME);
            _nodesCollections = _database.GetCollection<NodeEntity>(NODES_COLLECTION_NAME);
        }

        public TreeDto[] GetAllTrees()
        {
            return _treesCollections.Find(_ => true).ToEnumerable()
                .Select(e => e.ToDto()).ToArray();
        }

        public TreeDto GetTree(Guid guid)
        {
            return _treesCollections.Find(t => t.Guid == guid).FirstOrDefault()?.ToDto();
        }

        public TreeDto CreateTree(UpsertTreeDto tree)
        {
            var entity = new TreeEntity(
                Guid.NewGuid(),
                tree.Name,
                tree.Description,
                createdAt: DateTime.Now,
                updatedAt: DateTime.Now);

            _treesCollections.InsertOne(entity);
            return entity.ToDto();
        }

        public TreeDto UpdateTree(Guid guid, UpsertTreeDto upsert)
        {
            var updateBuilder = Builders<TreeEntity>.Update;
            var updates = new List<UpdateDefinition<TreeEntity>>();

            if (upsert.Name != null)
                updates.Add(updateBuilder.Set(t => t.Name, upsert.Name));

            if (upsert.Description != null)
                updates.Add(updateBuilder.Set(t => t.Description, upsert.Description));

            updates.Add(updateBuilder.Set(t => t.UpdatedAt, DateTime.Now));

            var tree = _treesCollections.FindOneAndUpdate<TreeEntity>(
                t => t.Guid == guid,
                updateBuilder.Combine(updates),
                new FindOneAndUpdateOptions<TreeEntity> { ReturnDocument = ReturnDocument.After });

            return tree?.ToDto();
        }

        public TreeDto DeleteTree(Guid guid)
        {
            return _treesCollections.FindOneAndDelete(t => t.Guid == guid)?.ToDto();
        }


        public NodeDto[] GetAllNodes(Guid treeGuid)
        {
            return _nodesCollections.Find(n => n.TreeGuid == treeGuid).ToEnumerable()
                .Select(e => e.ToDto()).ToArray();
        }

        public NodeDto GetNode(Guid guid)
        {
            return _nodesCollections.Find(t => t.Guid == guid).FirstOrDefault()?.ToDto();
        }

        public NodeDto CreateNode(Guid treeGuid, CreateNodeDto createNode)
        {
            // Check if tree exists
            var tree = GetTree(treeGuid);
            if (tree == null)
                return null;

            // Check if parent node exists (if given)
            var parentGuid = createNode.ParentGuid;
            if (parentGuid.HasValue && GetNode(parentGuid.Value) == null)
                return null;

            var entity = new NodeEntity(
                guid: Guid.NewGuid(),
                treeGuid: treeGuid,
                parentGuid: createNode.ParentGuid,
                name: createNode.Name,
                description: createNode.Description);

            _nodesCollections.InsertOne(entity);
            return entity.ToDto();
        }

        public NodeDto UpdateNode(Guid guid, UpdateNodeDto updateNode)
        {
            var updateBuilder = Builders<NodeEntity>.Update;
            var updates = new List<UpdateDefinition<NodeEntity>>();

            if (updateNode.Name != null)
                updates.Add(updateBuilder.Set(n => n.Name, updateNode.Name));

            if (updateNode.Description != null)
                updates.Add(updateBuilder.Set(n => n.Description, updateNode.Description));

            var tree = _nodesCollections.FindOneAndUpdate<NodeEntity>(
                t => t.Guid == guid,
                updateBuilder.Combine(updates),
                new FindOneAndUpdateOptions<NodeEntity> { ReturnDocument = ReturnDocument.After });

            return tree?.ToDto();
        }

        public NodeDto DeleteNode(Guid guid)
        {
            return _nodesCollections.FindOneAndDelete(t => t.Guid == guid)?.ToDto();
        }
    }
}
