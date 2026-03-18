using PaperForge.DAL.Entities;
using PaperForge.DAL.Enums;

namespace PaperForge.DAL.Seed;

public static class TemplateSeeder
{
    public static List<Template> GetTemplates() =>
    [
        new Template
        {
            Id = Guid.NewGuid(),
            Name = "Academic Essay",
            PaperType = PaperType.AcademicEssay,
            Description = "Standard academic essay with introduction, body paragraphs, and conclusion.",
            StructureJson = """
            [
                { "title": "Introduction", "wordTarget": 300, "guidance": "Present your topic and thesis statement. Provide background context and outline the structure of your essay. End with a clear thesis that states your main argument." },
                { "title": "Literature Review", "wordTarget": 500, "guidance": "Summarize and analyze relevant scholarly sources. Show how existing research relates to your topic. Identify gaps in the literature that your essay addresses." },
                { "title": "Main Argument", "wordTarget": 600, "guidance": "Present your primary argument with supporting evidence. Use topic sentences to introduce each point. Include citations to back up your claims." },
                { "title": "Counter-Arguments", "wordTarget": 400, "guidance": "Address opposing viewpoints fairly. Explain why your position is stronger. Use evidence to refute counter-arguments." },
                { "title": "Conclusion", "wordTarget": 250, "guidance": "Restate your thesis in light of the evidence presented. Summarize key points. Discuss implications and suggest areas for further research." }
            ]
            """
        },
        new Template
        {
            Id = Guid.NewGuid(),
            Name = "Scientific Report",
            PaperType = PaperType.ScientificReport,
            Description = "Structured scientific report following the IMRaD format.",
            StructureJson = """
            [
                { "title": "Abstract", "wordTarget": 250, "guidance": "Provide a concise summary of the entire report: purpose, methods, key findings, and conclusions. Write this section last." },
                { "title": "Introduction", "wordTarget": 400, "guidance": "State the research problem and its significance. Review relevant literature. Clearly state your hypothesis or research question." },
                { "title": "Methods", "wordTarget": 500, "guidance": "Describe your research methodology in detail. Include participants, materials, procedures, and data analysis methods. Be specific enough for replication." },
                { "title": "Results", "wordTarget": 500, "guidance": "Present your findings objectively without interpretation. Use tables and figures where appropriate. Report statistical analyses if applicable." },
                { "title": "Discussion", "wordTarget": 600, "guidance": "Interpret your results in relation to your hypothesis. Compare with existing literature. Discuss limitations and suggest future research directions." },
                { "title": "Conclusion", "wordTarget": 200, "guidance": "Summarize the main findings and their significance. State whether your hypothesis was supported. Highlight practical implications." }
            ]
            """
        },
        new Template
        {
            Id = Guid.NewGuid(),
            Name = "Literature Review",
            PaperType = PaperType.LiteratureReview,
            Description = "Comprehensive review and synthesis of existing research on a topic.",
            StructureJson = """
            [
                { "title": "Introduction", "wordTarget": 350, "guidance": "Define your topic and scope. Explain the significance of the review. State your research questions or objectives. Describe your search strategy and selection criteria." },
                { "title": "Thematic Analysis", "wordTarget": 800, "guidance": "Organize sources by themes, not chronologically. Compare and contrast different perspectives. Identify patterns, agreements, and contradictions in the literature." },
                { "title": "Methodological Review", "wordTarget": 400, "guidance": "Evaluate the research methods used across studies. Discuss strengths and weaknesses of different approaches. Note any methodological trends." },
                { "title": "Gaps and Future Directions", "wordTarget": 350, "guidance": "Identify what is missing from the current literature. Suggest areas that need further investigation. Explain why these gaps matter." },
                { "title": "Conclusion", "wordTarget": 250, "guidance": "Synthesize the key findings from your review. Restate the significance of the topic. Provide recommendations based on the literature." }
            ]
            """
        },
        new Template
        {
            Id = Guid.NewGuid(),
            Name = "Case Study",
            PaperType = PaperType.CaseStudy,
            Description = "In-depth analysis of a specific case or example.",
            StructureJson = """
            [
                { "title": "Introduction", "wordTarget": 300, "guidance": "Introduce the case and its context. Explain why this case is significant or interesting. State the purpose of the study." },
                { "title": "Background", "wordTarget": 400, "guidance": "Provide detailed background information about the case. Include relevant history, context, and stakeholders. Present any theoretical framework you are using." },
                { "title": "Analysis", "wordTarget": 700, "guidance": "Analyze the case using your chosen framework. Present evidence and data. Identify key issues, causes, and effects." },
                { "title": "Recommendations", "wordTarget": 400, "guidance": "Propose solutions or actions based on your analysis. Support recommendations with evidence. Consider feasibility and potential outcomes." },
                { "title": "Conclusion", "wordTarget": 200, "guidance": "Summarize the key findings. Reflect on lessons learned. Discuss broader implications of the case." }
            ]
            """
        },
        new Template
        {
            Id = Guid.NewGuid(),
            Name = "Argumentative Paper",
            PaperType = PaperType.ArgumentativePaper,
            Description = "Paper that takes a position and argues for it with evidence.",
            StructureJson = """
            [
                { "title": "Introduction", "wordTarget": 300, "guidance": "Hook the reader with a compelling opening. Provide background on the issue. Present a clear, debatable thesis statement." },
                { "title": "Background and Context", "wordTarget": 400, "guidance": "Provide necessary context for your argument. Define key terms. Explain the current state of the debate." },
                { "title": "Supporting Arguments", "wordTarget": 700, "guidance": "Present your strongest arguments with evidence. Use one paragraph per major point. Include data, expert opinions, and examples." },
                { "title": "Counterarguments and Rebuttals", "wordTarget": 500, "guidance": "Present the strongest opposing arguments fairly. Refute each with evidence and reasoning. Show why your position is more convincing." },
                { "title": "Conclusion", "wordTarget": 250, "guidance": "Restate your thesis and summarize key arguments. End with a call to action or thought-provoking statement. Leave a lasting impression." }
            ]
            """
        },
        new Template
        {
            Id = Guid.NewGuid(),
            Name = "Comparative Analysis",
            PaperType = PaperType.ComparativeAnalysis,
            Description = "Systematic comparison of two or more subjects.",
            StructureJson = """
            [
                { "title": "Introduction", "wordTarget": 300, "guidance": "Introduce the subjects being compared. Explain why the comparison is meaningful. State your thesis about the relationship between the subjects." },
                { "title": "Subject A Analysis", "wordTarget": 500, "guidance": "Provide a thorough analysis of the first subject. Cover key characteristics, strengths, and weaknesses. Use specific evidence and examples." },
                { "title": "Subject B Analysis", "wordTarget": 500, "guidance": "Analyze the second subject using the same criteria as Subject A. Maintain parallel structure for easy comparison. Highlight distinctive features." },
                { "title": "Comparative Discussion", "wordTarget": 600, "guidance": "Draw direct comparisons between the subjects. Identify similarities and differences. Analyze what these comparisons reveal about your thesis." },
                { "title": "Conclusion", "wordTarget": 250, "guidance": "Summarize key comparisons. State which subject better meets the criteria (if applicable). Discuss broader implications of your findings." }
            ]
            """
        }
    ];
}
