Throughout the year we add issues to the `Backlog` milestone as is pointed out in our [Triage Process](./docs/TriageProcess.md).
We review all the issues in that milestone once a year, after the work on an upcoming major release is complete.
Given the large number of issue, it takes multiple sessions for teams to review and identify candidates for consideration for the next major release.
This document details the process we use for identifying candidate issues for the next release.

## Phases
The process for identifying candidates for the next major release consistes of multiple phases, each one filtering out issues by leaving the min backlog or closing them.
- Filtering & Individual priorization
- Rough costing
- Team review & Priority adjustment
- Capacity planning
- Define the cut line

### Filtering
At this stage all the issues are distributed to engineers by feature areas.
The team agrees on some number of issues for each engineer to bring in to the next meeting with individual priority labels assigned  - fl-p1, fl-p2, fl-p3, where `fl` are the first letters of their first and last name.

Each engineer then goes through every single issue in the set they've been assigned to and labels those with individual priority labels.
All the issues which the engineer thinks is lower than p3 - remains on the backlog. We also agree to approximately balance the distribution of the 3 priroity labels on the issues that will be brough back by each engineer, so that it forces real prioritization excercise.
The issues which engineers think are good candidates and fit in the above listed requirements are moved to the `.NET V Planning` milestone, where `V` is the updcoming version number (7 at the time of this writing) - `.NET 7 Planning`.

### Rough costing
At this phase engineers apply rough cost estimates to the final list of issues that they have moved to the `.NET V Planning` milestone, by applying one of the `Cost: X` labels, where X is the size:
![image](https://user-images.githubusercontent.com/34246760/139494632-2a5145f6-eec9-40d6-919f-3ece8b9c986a.png)

This will be used later during the planning process.

### Team Review & Priority adjustment
Now, that all the issues are in the `.NET V planning` milestone, the team reviewes each issues one at a time starting from the highest priority ones (P1).
We discuss the issues and agree on the priority at this point. Sometimes we make adjustments to the suggested individual priorities. After discussing each issue the `Priority: X` label is applied to each issue.
Each `Priority: 1` issue is then moved to the project board, which will be used by each team for tracking the work for the upcoming release throughout the year. The project board has a `Triage` column and that's where all these issues land at this point.
At this point we bring to the board only the top prioritiy issues.

### Capacity Planning
We usually reserve only 50% of the team capacity for this work. The reason is that we will be getting a lot of incoming feedback throughout the year and we need to allocate time for handling this feedback throughout the year.
So we calculate the capacity of the team in weeks for the upcoming year and use half of the final number later in this process.

### Define the cut line
At this point we have all the candidate issues that we think are worth considering for the upcoming release. This number is quite large, so the teams usually won't have enough capacity to handle all this.
We start stack ranking issues so the most important work remains on the top of the list. We then draw the cut line and that defines the rough list of things the team will work on during the upcoming release.

