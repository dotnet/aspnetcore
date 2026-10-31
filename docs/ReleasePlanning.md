# Release planning

Throughout the year we add issues to the `Backlog` milestone as is pointed out in our [Triage Process](./TriageProcess.md).
We review all the issues in that milestone once a year, after the work on an upcoming major release is complete.
Given the large number of issues, it takes multiple sessions for teams to review and identify candidates for consideration for the next major release.
This document details the process we use for identifying candidate issues for the next release.

## Phases
The process for identifying candidates for the next major release consists of multiple phases. In each phase, we filter issues out of the release by either moving them to the `backlog`, or closing the issue.
- Filtering & Individual prioritization
- Rough costing
- Team review & Priority adjustment
- Capacity planning
- Define the cut line

### Filtering
At this stage all the issues are distributed to engineers by feature areas. Each engineer reviews all the issues within their feature area, and returns to the next meeting with individual priority labels assigned - fl-p1, fl-p2, fl-p3, where `fl` are their initials.

All the issues which the engineer believes are lower than `Priority-3` - remain in the backlog. We also agree to approximately balance the distribution of the 3 priority labels on the issues that will be brought back by each engineer, so that it forces real prioritization exercise.
The issues which engineers think are good candidates and fit in the above listed requirements are moved to the `.NET V Planning` milestone, where `V` is the upcoming version number (7 at the time of this writing) - `.NET 7 Planning`.

### Rough costing
At this phase engineers apply rough cost estimates to the final list of issues that they have moved to the `.NET V Planning` milestone, by applying one of the `Cost: X` labels below, where X is the size:
![image](https://user-images.githubusercontent.com/34246760/139494632-2a5145f6-eec9-40d6-919f-3ece8b9c986a.png)

This will be used later during the planning process.

For issues which don't have a clear description of the associated work, it's important to drop a comment summarizing the work involved. This will help at a later time, in case a question about the cost will be raised.

**Note**: while costing issues, it's important to reevaluate costs for those, which already have cost labels applied. Those are most probably from the past and may be outdated, not properly representing the cost anymore.

### Team Review & Priority adjustment
Now, that all the issues are in the `.NET V planning` milestone, the team reviews each issue one at a time starting from the highest priority ones (Priority: 1).
We discuss the issues and agree on the priority at this point. Sometimes we make adjustments to the suggested individual priorities. After discussing each issue the `Priority: X` label is applied to each issue.
Each `Priority: 1` issue is then moved to the project board, which will be used by each team for tracking the work for the upcoming release throughout the year. The issues start off in the `Triage` column. At this point we bring only the top priority issues to the board.

### Capacity Planning
We usually reserve only 50% of the team capacity for this work. The reason is that we will be getting a lot of incoming feedback throughout the year and we need to allocate time for handling this feedback throughout the year.
So we calculate the capacity of the team in weeks for the upcoming year and use half of the final number later in this process.

### Define the cut line
At this point we have all the candidate issues that we think are worth considering for the upcoming release. This number is quite large, so the teams usually won't have enough capacity to handle all this.
We start stack ranking issues so the most important work remains on the top of the list. We then draw the cut line and that defines the rough list of things the team will work on during the upcoming release.
