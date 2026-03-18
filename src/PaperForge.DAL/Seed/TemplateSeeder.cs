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
                { "title": "Introduction", "wordTarget": 0, "guidance": "Hook the reader and introduce your topic. Provide necessary background context. Narrow down to your specific focus. End with a clear, arguable thesis statement that previews your main points.\n\nTips:\n• Start with a surprising fact, question, or brief anecdote\n• Move from general context to your specific topic (funnel approach)\n• Your thesis should be specific, debatable, and supportable with evidence\n• Avoid dictionary definitions and overly broad openings" },
                { "title": "Literature Review", "wordTarget": 0, "guidance": "Summarize and analyze relevant scholarly sources. Organize by theme, not source-by-source. Show how existing research relates to your topic and identify gaps your essay addresses.\n\nTips:\n• Group sources by theme or argument, not chronologically\n• Use signal phrases: 'According to Smith (2020)...' or 'Research suggests (Jones, 2019)...'\n• Critically evaluate sources — don't just summarize\n• Show connections and contradictions between sources\n• Identify what's missing that your paper addresses" },
                { "title": "Main Argument", "wordTarget": 0, "guidance": "Present your primary argument with supporting evidence. Each paragraph should focus on one key point with a clear topic sentence, evidence, and analysis.\n\nTips:\n• Start each paragraph with a topic sentence that connects to your thesis\n• Use the PIE method: Point, Illustration (evidence), Explanation\n• Integrate quotes smoothly — never drop them in without context\n• Analyze evidence; don't just present it. Explain WHY it supports your point\n• Use transition words to connect ideas between paragraphs" },
                { "title": "Counter-Arguments", "wordTarget": 0, "guidance": "Address opposing viewpoints fairly and explain why your position is stronger. This strengthens your argument by showing you've considered multiple perspectives.\n\nTips:\n• Present the strongest version of the opposing argument (steel-man, not straw-man)\n• Use phrases like 'Critics argue that...' or 'Some scholars contend...'\n• Refute with evidence, not just opinion\n• Concede valid points where appropriate — this shows intellectual honesty\n• Explain why your position still holds despite the counterargument" },
                { "title": "Conclusion", "wordTarget": 0, "guidance": "Synthesize your argument and leave a lasting impression. Don't just restate — show why your argument matters.\n\nTips:\n• Restate your thesis in new words, reflecting the evidence presented\n• Summarize key arguments without introducing new evidence\n• Discuss broader implications: Why does this matter? What are the consequences?\n• End with a call to action, future research direction, or thought-provoking statement\n• Avoid phrases like 'In conclusion' — just conclude naturally" }
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
                { "title": "Abstract", "wordTarget": 0, "guidance": "Provide a concise summary of the entire report: purpose, methods, key findings, and conclusions. Write this section LAST.\n\nTips:\n• Keep to 150-250 words (check your assignment requirements)\n• Include: background, objective, methods, results, and conclusion\n• Do not cite references in the abstract\n• Use past tense for what was done, present tense for conclusions\n• Include 3-5 keywords below the abstract" },
                { "title": "Introduction", "wordTarget": 0, "guidance": "State the research problem and its significance. Review relevant literature. Clearly state your hypothesis or research question.\n\nTips:\n• Start broad (why this field matters) then narrow to your specific question\n• Cite key studies that inform your research\n• Clearly state what gap in knowledge your study addresses\n• End with your hypothesis or research question\n• Use present tense for established facts, past tense for previous studies" },
                { "title": "Methods", "wordTarget": 0, "guidance": "Describe your methodology in enough detail for replication. Include participants, materials, procedures, and analysis methods.\n\nTips:\n• Use past tense throughout\n• Organize with subheadings: Participants, Materials, Procedure, Data Analysis\n• Be specific: exact quantities, durations, instruments used\n• Mention ethical approvals if applicable\n• Don't include results here — only what you DID, not what you FOUND" },
                { "title": "Results", "wordTarget": 0, "guidance": "Present findings objectively without interpretation. Use tables and figures where appropriate. Report statistical analyses.\n\nTips:\n• Use past tense to describe results\n• Lead with the most important findings\n• Reference tables/figures in the text: 'As shown in Table 1...'\n• Report exact statistics: F(1, 45) = 4.56, p = .038\n• State both significant AND non-significant results\n• Don't interpret here — save that for Discussion" },
                { "title": "Discussion", "wordTarget": 0, "guidance": "Interpret your results in relation to your hypothesis and existing literature. Discuss limitations and future directions.\n\nTips:\n• Start by stating whether your hypothesis was supported\n• Compare your findings with previous studies\n• Explain unexpected results\n• Acknowledge limitations honestly\n• Suggest practical implications\n• Propose future research directions" },
                { "title": "Conclusion", "wordTarget": 0, "guidance": "Summarize the main findings and their significance in 1-2 paragraphs.\n\nTips:\n• Briefly restate the purpose and key findings\n• State the main takeaway\n• Highlight practical applications\n• Keep it concise — this is a summary, not a new discussion" }
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
                { "title": "Introduction", "wordTarget": 0, "guidance": "Define your topic and scope. Explain the significance of the review and state your research questions.\n\nTips:\n• Clearly define the scope: time period, geographic focus, disciplines covered\n• Explain why this review is needed now\n• State your research questions or objectives\n• Briefly describe your search strategy and databases used\n• Preview the organizational structure of your review" },
                { "title": "Thematic Analysis", "wordTarget": 0, "guidance": "Organize sources by themes, not chronologically or source-by-source. Compare, contrast, and synthesize findings.\n\nTips:\n• Use thematic subheadings to organize related studies\n• Synthesize — don't just summarize each source separately\n• Show patterns: 'Multiple studies confirm...' or 'Findings are mixed regarding...'\n• Note methodological strengths and weaknesses across studies\n• Use a literature matrix to track themes across sources" },
                { "title": "Methodological Review", "wordTarget": 0, "guidance": "Evaluate the research methods used across studies. Note trends, strengths, and weaknesses.\n\nTips:\n• Compare sample sizes, research designs, and measurement tools\n• Note which methods produced the most reliable results\n• Identify methodological limitations common across studies\n• Suggest more rigorous approaches for future research" },
                { "title": "Gaps and Future Directions", "wordTarget": 0, "guidance": "Identify what is missing from the current literature and suggest areas for further investigation.\n\nTips:\n• Be specific about what gaps exist and why they matter\n• Suggest concrete research questions for future studies\n• Explain how filling these gaps would advance the field\n• Connect gaps to real-world needs or applications" },
                { "title": "Conclusion", "wordTarget": 0, "guidance": "Synthesize the key findings from your review and provide recommendations.\n\nTips:\n• Summarize the overall state of knowledge on your topic\n• Highlight the most important themes and findings\n• Restate the most critical gaps\n• End with the broader significance of the research area" }
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
                { "title": "Introduction", "wordTarget": 0, "guidance": "Introduce the case and explain why it's significant.\n\nTips:\n• Briefly describe the case and its context\n• Explain why this particular case was chosen\n• State the purpose and scope of your analysis\n• Preview the theoretical framework you'll use" },
                { "title": "Background", "wordTarget": 0, "guidance": "Provide detailed context about the case including history, stakeholders, and relevant theory.\n\nTips:\n• Include relevant timeline of events\n• Identify key stakeholders and their roles\n• Present the theoretical/conceptual framework\n• Use credible sources to support background information" },
                { "title": "Analysis", "wordTarget": 0, "guidance": "Analyze the case using your chosen framework. Present evidence and identify key issues.\n\nTips:\n• Apply your theoretical framework systematically\n• Identify root causes, not just symptoms\n• Use data and evidence to support your analysis\n• Consider multiple perspectives and interpretations\n• Use subheadings for different aspects of the analysis" },
                { "title": "Recommendations", "wordTarget": 0, "guidance": "Propose actionable solutions based on your analysis.\n\nTips:\n• Make recommendations specific, measurable, and realistic\n• Prioritize: what should be done first?\n• Support each recommendation with evidence from your analysis\n• Consider potential obstacles and how to overcome them\n• Include both short-term and long-term recommendations" },
                { "title": "Conclusion", "wordTarget": 0, "guidance": "Summarize key findings, lessons learned, and broader implications.\n\nTips:\n• Recap the most important insights from your analysis\n• Explain what can be learned from this case\n• Discuss how these lessons apply to similar situations\n• Keep it brief and impactful" }
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
                { "title": "Introduction", "wordTarget": 0, "guidance": "Hook the reader, provide background, and present a clear thesis.\n\nTips:\n• Open with a compelling hook: startling statistic, provocative question, or vivid scenario\n• Provide just enough context for readers to understand the issue\n• Present your thesis as the last sentence — it should be specific and debatable\n• Avoid: 'In this paper I will...' — just make your argument directly" },
                { "title": "Background and Context", "wordTarget": 0, "guidance": "Provide necessary context and define key terms.\n\nTips:\n• Explain the history or origin of the issue\n• Define any technical or contested terms\n• Present the current state of the debate objectively\n• Show why this issue matters right now\n• Use this section to establish your credibility on the topic" },
                { "title": "Supporting Arguments", "wordTarget": 0, "guidance": "Present your strongest arguments with evidence.\n\nTips:\n• Lead with your second-strongest argument, end with your strongest\n• One major argument per paragraph\n• Use varied evidence: statistics, expert testimony, case studies, logical reasoning\n• Explain the significance of each piece of evidence\n• Connect each argument back to your thesis\n• Use strong transition phrases between paragraphs" },
                { "title": "Counterarguments and Rebuttals", "wordTarget": 0, "guidance": "Address opposing views and explain why your position is stronger.\n\nTips:\n• Present 1-2 serious counterarguments (don't pick weak ones)\n• Acknowledge what's valid in the opposing view\n• Refute with specific evidence and reasoning\n• Show how your position accounts for the counterargument\n• This section demonstrates critical thinking and strengthens your credibility" },
                { "title": "Conclusion", "wordTarget": 0, "guidance": "Drive your argument home and leave a lasting impact.\n\nTips:\n• Restate your thesis with the confidence of someone who has proved it\n• Briefly echo your strongest evidence\n• Explain the real-world significance or consequences\n• End with a call to action or powerful closing statement\n• Never introduce new arguments in the conclusion" }
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
                { "title": "Introduction", "wordTarget": 0, "guidance": "Introduce the subjects being compared and explain why the comparison matters.\n\nTips:\n• Clearly identify what you're comparing and why\n• State the criteria you'll use for comparison\n• Present your thesis: what does this comparison reveal?\n• Specify your organizational approach (point-by-point or subject-by-subject)" },
                { "title": "Subject A Analysis", "wordTarget": 0, "guidance": "Provide a thorough analysis of the first subject using your stated criteria.\n\nTips:\n• Cover each comparison criterion systematically\n• Use specific evidence, examples, and data\n• Be objective — present strengths AND weaknesses\n• Use the same structure you'll use for Subject B to enable easy comparison" },
                { "title": "Subject B Analysis", "wordTarget": 0, "guidance": "Analyze the second subject using the SAME criteria as Subject A.\n\nTips:\n• Mirror the structure of your Subject A analysis\n• Use parallel language to make comparison clear\n• Begin connecting to Subject A where natural\n• Maintain the same level of depth and detail" },
                { "title": "Comparative Discussion", "wordTarget": 0, "guidance": "Draw direct comparisons and analyze what the differences/similarities reveal.\n\nTips:\n• Go criterion by criterion, comparing directly\n• Don't just list differences — analyze what they mean\n• Use comparison language: 'while...', 'in contrast...', 'similarly...'\n• Connect your findings to your thesis\n• Discuss which subject better meets each criterion and why" },
                { "title": "Conclusion", "wordTarget": 0, "guidance": "Summarize key findings and state your overall assessment.\n\nTips:\n• Restate your thesis in light of the comparison\n• Highlight the most important similarities and differences\n• State which subject is 'better' if your thesis supports it\n• Discuss broader implications of your findings\n• Suggest when each subject might be preferable" }
            ]
            """
        }
    ];
}
