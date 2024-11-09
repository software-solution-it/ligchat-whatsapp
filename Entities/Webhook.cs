using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Eon.Backend.Domain.Entities
{
    /// <summary>
    /// Representa a entidade de webhook no sistema.
    /// Mapeia a tabela "webhooks" no banco de dados e herda de BaseEntity.
    /// </summary>
    [Table("webhooks")]
    public class Webhook
    {
        /// <summary>
        /// Identificador único do webhook.
        /// Este campo é a chave primária da tabela.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Nome do webhook.
        /// Representa o nome pelo qual o webhook é identificado.
        /// </summary>
        [Column("nome")]
        [Required]
        [MaxLength(100)] // Limita o comprimento do nome a 100 caracteres
        public string Name { get; set; }

        /// <summary>
        /// URL do webhook.
        /// Endereço para o qual as solicitações de webhook serão enviadas.
        /// </summary>
        [Column("callback_url")]
        [Required]
        [MaxLength(2048)] // Limita o comprimento da URL a 2048 caracteres
        public string CallbackUrl { get; set; }

        /// <summary>
        /// Identificador do setor associado ao webhook.
        /// Este campo é opcional e representa o ID do setor ao qual o webhook pode pertencer.
        /// </summary>
        [Column("id_setor")]
        public int? SectorId { get; set; }


        /// <summary>
        /// Data e hora de criação do webhook.
        /// Este campo é utilizado para registrar quando o webhook foi criado.
        /// </summary>
        [Column("data_criacao")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Inicializa CreatedAt com a data e hora atuais em UTC

        /// <summary>
        /// Data e hora da última atualização do webhook.
        /// Este campo é utilizado para registrar quando o webhook foi atualizado pela última vez.
        /// </summary>
        [Column("data_atualizacao")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Inicializa UpdatedAt com a data e hora atuais em UTC

        /// <summary>
        /// Construtor padrão da classe Webhook.
        /// Inicializa as propriedades com valores padrão.
        /// </summary>
        public Webhook()
        {
            // A inicialização padrão é definida nas propriedades diretamente.
        }

        /// <summary>
        /// Construtor da classe Webhook que inicializa a entidade com valores específicos.
        /// </summary>
        /// <param name="name">Nome do webhook.</param>
        /// <param name="callbackUrl">URL do webhook.</param>
        /// <param name="sectorId">Identificador do setor associado ao webhook.</param>
        public Webhook(string name, string callbackUrl, int? sectorId = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            CallbackUrl = callbackUrl ?? throw new ArgumentNullException(nameof(callbackUrl));
            SectorId = sectorId; // Inicializa o SectorId com o valor fornecido ou null
            CreatedAt = DateTime.UtcNow; // Inicializa CreatedAt com a data e hora atuais em UTC
            UpdatedAt = DateTime.UtcNow; // Inicializa UpdatedAt com a data e hora atuais em UTC
        }
    }
}
