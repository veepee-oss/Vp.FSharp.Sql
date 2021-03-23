# Contributing to `Vp.FSharp.Sql`

Please take a moment to review this document in order to make the contribution process easy and effective for everyone involved!

## Code of Conduct
### Our Pledge
In the interest of fostering an open and welcoming environment, we as contributors and maintainers pledge to making participation in our project and our community a harassment-free experience for everyone, regardless of age, body size, disability, ethnicity, gender identity and expression, level of experience, nationality, personal appearance, race, religion, or sexual identity and orientation.

### Our Standards
Examples of behavior that contributes to creating a positive environment include:

- Using welcoming and inclusive language
- Being respectful of differing viewpoints and experiences
- Gracefully accepting constructive criticism
- Focusing on what is best for the community
- Showing empathy towards other community members

Examples of unacceptable behavior by participants include:
- The use of sexualized language or imagery and unwelcome sexual attention or advances
- Trolling, insulting/derogatory comments, and personal or political attacks
- Public or private harassment
- Publishing others' private information, such as a physical or electronic address, without explicit permission
- Other conduct which could reasonably be considered inappropriate in a professional setting

### Our Responsibilities
- Project maintainers are responsible for clarifying the standards of acceptable behavior and are expected to take appropriate and fair corrective action in response to any instances of unacceptable behavior.

Project maintainers have the right and responsibility to remove, edit, or reject comments, commits, code, wiki edits, issues, and other contributions that are not aligned to this Code of Conduct, or to ban temporarily or permanently any contributor for other behaviors that they deem inappropriate, threatening, offensive, or harmful.

### Scope
This Code of Conduct applies both within project spaces and in public spaces when an individual is representing the project or its community. Examples of representing a project or community include using an official project e-mail address, posting via an official social media account, or acting as an appointed representative at an online or offline event. Representation of a project may be further defined and clarified by project maintainers.

### Enforcement
Instances of abusive, harassing, or otherwise unacceptable behavior may be reported by contacting the project team at [INSERT EMAIL ADDRESS]. All complaints will be reviewed and investigated and will result in a response that is deemed necessary and appropriate to the circumstances. The project team is obligated to maintain confidentiality with regard to the reporter of an incident. Further details of specific enforcement policies may be posted separately.

Project maintainers who do not follow or enforce the Code of Conduct in good faith may face temporary or permanent repercussions as determined by other members of the project's leadership.

## Using the issue tracker

Use the issues tracker for:

- bug reports
- feature requests
- submitting pull requests

## Bug Reports

A bug is either a _demonstrable problem_ that is caused by the code in the repository, or indicate missing, unclear, or misleading documentation. 

**Good bug reports are extremely helpful - thank you!**

Guidelines for bug reports:

1. Use the GitHub issue search — check if the issue has already been reported.
2. Check if the issue has been fixed — try to reproduce it using the `main` branch in the repository.
3. Report the problem — ideally create a reduced test case.

Please try to be as detailed as possible in your report. Include information about your Operating System, as well as your dotnet (or mono \ .Net Framework), F# and project versions. Please provide steps to reproduce the issue as well as the outcome you were expecting! All these details will help developers to fix any potential bugs.

Example:

> Short and descriptive example bug report title
> 
> A summary of the issue and the environment in which it occurs. If suitable, include the steps required to reproduce the bug.
> 
> 1. This is the first step
> 2. This is the second step
> 3. Further steps, etc.
> `<url>` - a link to the reduced test case (e.g. a GitHub Gist)
> 
> Any other information you want to share that is relevant to the issue being reported. This might include the lines of code that you have identified as causing the bug, and potential solutions (and your opinions on their merits).

## Feature requests
Feature requests are welcome and should be discussed on issue tracker. But take a moment to find out whether your idea fits with the scope and aims of the project. It's up to you to make a strong case to convince the community of the merits of this feature. Please provide as much detail and context as possible.

## Pull requests
Good pull requests - patches, improvements, new features - are a fantastic help. They should remain focused in scope and avoid containing unrelated commits.

**IMPORTANT**: By submitting a patch, you agree that your work will be licensed under the license used by the project (ie. MIT).

If you have any large pull request in mind (e.g. implementing features, refactoring code, etc), please ask first otherwise you risk spending a lot of time working on something that the project's developers might not want to merge into the project.

Please adhere to the coding conventions in the project (indentation, accurate comments, etc.) and don't forget to add your own tests and documentation. When working with git, we recommend the following process in order to craft an excellent pull request:

1. [Fork](https://help.github.com/articles/fork-a-repo/) the project, clone your fork,  and configure the remotes:

   ```bash
   # Clone your fork of the repo into the current directory
   git clone https://github.com/<your-username>/Vp.FSharp.Sql
   # Navigate to the newly cloned directory
   cd Vp.FSharp.Sql
   # Assign the original repo to a remote called "upstream"
   git remote add upstream https://github.com/fsprojects/Vp.FSharp.Sql
   ```

2. If you cloned a while ago, get the latest changes from upstream, and update your fork:

   ```bash
   git checkout main
   git pull upstream main
   git push
   ```

3. Create a new topic branch (off of `main`) to contain your feature, change, or fix.

   **IMPORTANT**: Making changes in `main` is discouraged. You should always keep your local `main` in sync with upstream `main` and make your changes in topic branches.

   ```bash
   git checkout -b <topic-branch-name>
   ```

4. Commit your changes in logical chunks. Keep your commit messages organized, with a short description in the first line and more detailed information on the following lines. Feel free to use Git's [interactive rebase](https://help.github.com/articles/about-git-rebase/) feature to tidy up your commits before making them public.

5. Make sure all the tests are still passing.

   ```bash
   dotnet tool restore
   dotnet fake build
   ```

6. Push your topic branch up to your fork:

   ```bash
   git push origin <topic-branch-name>
   ```

7. [Open a Pull Request](https://help.github.com/articles/about-pull-requests/) with a clear title and description.

8. If you haven't updated your pull request for a while, you should consider rebasing on `main` and resolving any conflicts.

   **IMPORTANT**: _Never ever_ merge upstream `main` into your branches. You should always `git rebase` on `main` to bring your changes up to date when necessary.

   ```bash
   git checkout main
   git pull upstream main
   git checkout <your-topic-branch>
   git rebase main
   ```
