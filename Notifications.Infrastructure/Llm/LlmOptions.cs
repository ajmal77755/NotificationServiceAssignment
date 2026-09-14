using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Notifications.Infrastructure.Llm
{
    public sealed class LlmOptions
    {
        public const string SectionName = "Llm";

        [Required]
        public string ApiKey { get; set; } = string.Empty;

        [Required] 
        public string Model { get; set; } = string.Empty;

        [Range(0, 1)]
        public float Temperature { get; set; } = 0.2f;


    }
}
