using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WhatsAppProject.Entities
{
    public class FlowDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("flowIn")]
        public string FlowIn { get; set; } // Campo adicionado conforme necessário

        [BsonElement("name")]
        public string Name { get; set; } // Nome do flow

        [BsonElement("description")]
        public string Description { get; set; } // Descrição do flow

        [BsonElement("sectorId")]
        public int SectorId { get; set; } // ID do setor associado ao flow

        [BsonElement("nodes")]
        public List<NodeDTO> Nodes { get; set; } // Lista de nós

        [BsonElement("edges")]
        public List<EdgeDTO> Edges { get; set; } // Lista de arestas
    }

    public class NodeDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } // Identificador único do nó no MongoDB

        [BsonElement("label")]
        public string Label { get; set; } // Rótulo do nó

        [BsonElement("type")]
        public string Type { get; set; }

        [BsonElement("blocks")]
        public List<BlockDTO> Blocks { get; set; } // Lista de blocos

        [BsonElement("menuOptions")]
        public List<MenuOptionDTO> MenuOptions { get; set; } // Lista de opções de menu

        [BsonElement("selectedTag")]
        public TagDTO? SelectedTag { get; set; } // Tag selecionada, se houver

        [BsonElement("condition")]
        public ConditionDTO? Condition { get; set; } // Condição associada ao nó, se houver
    }

    public class BlockDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } // Identificador único do bloco no MongoDB

        [BsonElement("type")]
        public string Type { get; set; } // Tipo de bloco

        [BsonElement("content")]
        public string Content { get; set; } // Conteúdo do bloco

        [BsonElement("media")]
        public MediaDTO? Media { get; set; } // Mídia associada ao bloco, se houver

        [BsonElement("duration")]
        public int? Duration { get; set; } // Duração do bloco, se aplicável
    }

    public class MenuOptionDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } // Identificador único da opção de menu no MongoDB

        [BsonElement("title")]
        public string Title { get; set; } // Título da opção de menu

        [BsonElement("content")]
        public List<string> Content { get; set; } // Conteúdo da opção de menu
    }

    public class TagDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } // Identificador único da tag no MongoDB

        [BsonElement("tagId")]
        public int TagId { get; set; } // ID da tag
    }

    public class ConditionDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } // Identificador único da condição no MongoDB

        [BsonElement("variableId")]
        public int VariableId { get; set; } // ID da variável associada à condição

        [BsonElement("condition")]
        public string Condition { get; set; } // Condição a ser avaliada

        [BsonElement("value")]
        public string Value { get; set; } // Valor a ser comparado
    }

    public class MediaDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } // Identificador único da mídia no MongoDB

        [BsonElement("name")]
        public string Name { get; set; } // Nome da mídia

        [BsonElement("mimeType")]
        public string MimeType { get; set; } // Tipo MIME da mídia

        [BsonElement("base64")]
        public string Base64 { get; set; } // Representação Base64 da mídia
    }

    public class EdgeDTO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } // Identificador único da aresta no MongoDB

        [BsonElement("source")]
        public string Source { get; set; } // ID do nó de origem

        [BsonElement("target")]
        public string Target { get; set; } // ID do nó de destino

        [BsonElement("animated")]
        public bool Animated { get; set; } // Indica se a aresta é animada

        [BsonElement("sourceHandle")]
        public string? SourceHandle { get; set; } // Identificador do manipulador de origem, se houver

        [BsonElement("targetHandle")]
        public string? TargetHandle { get; set; } // Identificador do manipulador de destino, se houver
    }
}
