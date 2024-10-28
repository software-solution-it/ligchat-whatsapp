using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WhatsAppProject.Entities
{
    /// <summary>
    /// Representa a entidade Tag no sistema.
    /// Mapeia a tabela "etiquetas" no banco de dados e implementa a interface ITagEntityInterface.
    /// </summary>
    [Table("etiquetas")]
    public class Tag 
    {
        /// <summary>
        /// Identificador único da entidade.
        /// Deve ser um valor único para cada entidade e geralmente é gerado automaticamente.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Nome da tag.
        /// Este campo é obrigatório e representa o nome pelo qual a tag é conhecida.
        /// </summary>
        [Column("nome_etiqueta")]
        [Required]
        [MaxLength(100)] // Limita o comprimento do nome a 100 caracteres
        public string Name { get; set; }

        /// <summary>
        /// Descrição da tag.
        /// Utilizado para fornecer detalhes adicionais sobre a tag.
        /// Este campo é opcional.
        /// </summary>
        [Column("descricao_etiqueta")]
        public string? Description { get; set; }

        /// <summary>
        /// Identificador do setor associado à tag.
        /// Este campo é opcional e representa o ID do setor ao qual a tag pode pertencer.
        /// </summary>
        [Column("id_setor")]
        public int? SectorId { get; set; }

        /// <summary>
        /// Data e hora de criação da tag.
        /// Este campo é utilizado para registrar quando a tag foi criada.
        /// </summary>
        [Column("data_criacao")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data e hora da última atualização da tag.
        /// Este campo é utilizado para registrar quando a tag foi atualizada pela última vez.
        /// </summary>
        [Column("data_atualizacao")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Status da entidade.
        /// Indica se a entidade está ativa ou inativa.
        /// </summary>
        [Column("status")]
        public bool Status { get; set; } = true;

        /// <summary>
        /// Construtor padrão da classe Tag.
        /// Inicializa as propriedades com valores padrão.
        /// </summary>
        public Tag()
        {
            Name = string.Empty;
            Description = null; // Inicializa como nulo para representar a opcionalidade
            SectorId = null; // Inicializa como nulo para representar a opcionalidade
            CreatedAt = UpdatedAt = DateTime.UtcNow; // Inicializa as datas
        }

        /// <summary>
        /// Construtor da classe Tag que inicializa a tag com valores específicos.
        /// </summary>
        /// <param name="name">Nome da tag.</param>
        /// <param name="description">Descrição da tag (opcional).</param>
        /// <param name="sectorId">Identificador do setor associado (opcional).</param>
        public Tag(string name, string? description = null, int? sectorId = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name), "O nome não pode ser nulo ou vazio.");
            Description = description; // Pode ser nulo
            SectorId = sectorId; // Pode ser nulo
            CreatedAt = UpdatedAt = DateTime.UtcNow; // Inicializa as datas
        }
    }
}
